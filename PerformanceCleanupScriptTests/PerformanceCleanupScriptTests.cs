namespace PerformanceLoggerCleanupScript_1.Tests
{
	using System;
	using System.IO;
	using Moq;
	using Skyline.DataMiner.Automation;

	[TestClass]
	public class PerformanceCleanupScriptTests
	{
		private const string TestFolderPath = @"C:\Skyline_Data\PerformanceLogger"; // Ensure this path is suitable for testing.
		private Mock<IEngine> mockEngine;
		private Script script;

		[TestInitialize]
		public void Setup()
		{
			this.mockEngine = new Mock<IEngine>();
			this.script = new Script();
		}

		[TestMethod]
		public void Run_DirectoryNotFound_ExitsWithMessage()
		{
			var mockScriptParam = new Mock<ScriptParam>();
			mockScriptParam.Setup(sp => sp.Value).Returns("7");

			this.mockEngine.Setup(e => e.GetScriptParam(It.IsAny<string>())).Returns(mockScriptParam.Object);

			if (Directory.Exists(TestFolderPath))
			{
				Directory.Delete(TestFolderPath, true);
			}

			this.script.Run(this.mockEngine.Object);

			this.mockEngine.Verify(e => e.ExitFail(It.Is<string>(s => s.Contains("Directory not found"))), Times.Once);
		}

		[TestMethod]
		public void Run_AccessDenied_ExitsWithMessage()
		{
			var mockScriptParam = new Mock<ScriptParam>();
			mockScriptParam.Setup(sp => sp.Value).Returns("7");

			this.mockEngine.Setup(e => e.GetScriptParam(It.IsAny<string>())).Throws(new UnauthorizedAccessException("Simulated access denied error."));
			this.script.Run(this.mockEngine.Object);

			mockEngine.Verify(e => e.ExitFail(It.Is<string>(s => s.Contains("Access denied"))), Times.Once);
		}

		[TestMethod]
		public void Run_ValidParameters_DeletesOldFiles()
		{
			var mockScriptParam = new Mock<ScriptParam>();
			mockScriptParam.Setup(sp => sp.Value).Returns("7");
			this.mockEngine.Setup(e => e.GetScriptParam("Days of oldest performance info")).Returns(mockScriptParam.Object);

			Directory.CreateDirectory(TestFolderPath);
			File.WriteAllText(Path.Combine(TestFolderPath, "oldFile.txt"), "test content");
			File.SetLastWriteTime(Path.Combine(TestFolderPath, "oldFile.txt"), DateTime.Now.AddDays(-10));

			File.WriteAllText(Path.Combine(TestFolderPath, "newFile.txt"), "test content");
			File.SetLastWriteTime(Path.Combine(TestFolderPath, "newFile.txt"), DateTime.Now);

			this.script.Run(this.mockEngine.Object);

			Assert.IsFalse(File.Exists(Path.Combine(TestFolderPath, "oldFile.txt")), "Old file should be deleted.");
			Assert.IsTrue(File.Exists(Path.Combine(TestFolderPath, "newFile.txt")), "New file should still exist.");

			Directory.Delete(TestFolderPath, true);
		}

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