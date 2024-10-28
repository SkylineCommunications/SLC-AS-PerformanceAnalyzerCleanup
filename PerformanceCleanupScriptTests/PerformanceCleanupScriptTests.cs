namespace PerformanceLoggerCleanupScript_1.Tests
{
	using System;
	using System.IO;
	using Moq;
	using Skyline.DataMiner.Automation;

	/// <summary>
	/// Tests for Performance Cleanup Script.
	/// </summary>
	[TestClass]
	public class PerformanceCleanupScriptTests
	{
		private Mock<IEngine> mockEngine;
		private Script script;

		/// <summary>
		/// Test setup.
		/// </summary>
		[TestInitialize]
		public void Setup()
		{
			this.mockEngine = new Mock<IEngine>();
			this.script = new Script();
		}

		/// <summary>
		/// Verifies that the script exits with an error message when the specified folder does not exist.
		/// </summary>
		[TestMethod]
		public void Run_DirectoryNotFound_ExitsWithMessage()
		{
			var mockDaysParam = new Mock<ScriptParam>();
			mockDaysParam.Setup(sp => sp.Value).Returns("7");
			this.mockEngine.Setup(e => e.GetScriptParam(2)).Returns(mockDaysParam.Object);

			var mockFolderParam = new Mock<ScriptParam>();
			string nonExistingFolderPath = "C:\\Skyline_Data\\NonExistingFolder";
			mockFolderParam.Setup(sp => sp.Value).Returns(nonExistingFolderPath);
			this.mockEngine.Setup(e => e.GetScriptParam(3)).Returns(mockFolderParam.Object);

			if (Directory.Exists(nonExistingFolderPath))
			{
				Directory.Delete(nonExistingFolderPath, true);
			}

			this.script.Run(this.mockEngine.Object);

			this.mockEngine.Verify(e => e.ExitFail(It.Is<string>(s => s.Contains("Directory not found"))), Times.Once);
		}

		/// <summary>
		/// Validates that the script deletes old files while keeping recent ones when run with valid parameters.
		/// </summary>
		[TestMethod]
		public void Run_ValidParameters_DeletesOldFiles()
		{
			var mockDaysParam = new Mock<ScriptParam>();
			mockDaysParam.Setup(sp => sp.Value).Returns("7");
			this.mockEngine.Setup(e => e.GetScriptParam(2)).Returns(mockDaysParam.Object);

			var mockFolderParam = new Mock<ScriptParam>();
			string folderPath = "C:\\Skyline_Data\\PerformanceLogger"; // Define your actual folder path here
			mockFolderParam.Setup(sp => sp.Value).Returns(folderPath);
			this.mockEngine.Setup(e => e.GetScriptParam(3)).Returns(mockFolderParam.Object);

			Directory.CreateDirectory(folderPath);

			using (var stream = File.CreateText(Path.Combine(folderPath, "oldFile.txt")))
			{
				stream.Write("test content");
			}

			File.SetLastWriteTime(Path.Combine(folderPath, "oldFile.txt"), DateTime.Now.AddDays(-10));

			using (var stream = File.CreateText(Path.Combine(folderPath, "newFile.txt")))
			{
				stream.Write("test content");
			}

			File.SetLastWriteTime(Path.Combine(folderPath, "newFile.txt"), DateTime.Now);

			this.script.Run(this.mockEngine.Object);

			Assert.IsFalse(File.Exists(Path.Combine(folderPath, "oldFile.txt")), "Old file should be deleted.");
			Assert.IsTrue(File.Exists(Path.Combine(folderPath, "newFile.txt")), "New file should still exist.");

			var files = Directory.GetFiles(folderPath);
			foreach (var file in files)
			{
				var fileInfo = new FileInfo(file)
				{
					IsReadOnly = false,
				};
				fileInfo.Delete();
			}

			Directory.Delete(folderPath, true);
		}

		/// <summary>
		/// Confirms that an ArgumentException is thrown when an invalid days parameter is provided.
		/// </summary>
		[TestMethod]
		public void Initialize_InvalidDaysParameter_ThrowsArgumentException()
		{
			var mockScriptParam = new Mock<ScriptParam>();
			mockScriptParam.Setup(sp => sp.Value).Returns("invalid"); // Set to invalid input
			this.mockEngine.Setup(e => e.GetScriptParam("Days of oldest performance info")).Returns(mockScriptParam.Object);

			var exception = Assert.ThrowsException<ArgumentException>(() =>
			{
				this.script.Initialize(this.mockEngine.Object);
			});

			Assert.AreEqual("Invalid or missing value for Days of oldest performance info. It must be a valid integer.", exception.Message);
		}
	}
}