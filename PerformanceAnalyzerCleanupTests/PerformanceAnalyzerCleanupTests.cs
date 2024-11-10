namespace Skyline.DataMiner.Utilities.PerformanceAnalyzerCleanup.Tests
{
	using System;
	using System.IO;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Moq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utilities.PerformanceAnalyzerCleanup;

	[TestClass]
	public class PerformanceAnalyzerCleanupTests
	{
		private Mock<IEngine> mockEngine;
		private Script script;
		private string testDirectory;

		[TestInitialize]
		public void Setup()
		{
			mockEngine = new Mock<IEngine>();
			script = new Script();

			// Set up a temporary directory for file tests
			testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(testDirectory);
		}

		[TestCleanup]
		public void Cleanup()
		{
			// Clean up temporary directory
			if (Directory.Exists(testDirectory))
			{
				Directory.Delete(testDirectory, true);
			}
		}

		[TestMethod]
		public void PerformanceCleanupScriptTests_Run_ValidParameters_DeletesOldFiles()
		{
			// Arrange
			var mockDaysParam = new Mock<ScriptParam>();
			mockEngine.Setup(e => e.GetScriptParam("Max Days Since Last Modified")).Returns(mockDaysParam.Object);
			mockDaysParam.Setup(sp => sp.Value).Returns("7");

			var mockFolderParam = new Mock<ScriptParam>();
			mockEngine.Setup(e => e.GetScriptParam("Performance Metrics Location")).Returns(mockFolderParam.Object);
			mockFolderParam.Setup(sp => sp.Value).Returns(testDirectory);

			File.WriteAllText(Path.Combine(testDirectory, "oldFile.txt"), "test content");
			File.SetLastWriteTime(Path.Combine(testDirectory, "oldFile.txt"), DateTime.Now.AddDays(-10));

			File.WriteAllText(Path.Combine(testDirectory, "newFile.txt"), "test content");

			// Act
			script.Run(mockEngine.Object);

			// Assert
			Assert.IsFalse(File.Exists(Path.Combine(testDirectory, "oldFile.txt")), "Old file should be deleted.");
			Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "newFile.txt")), "New file should still exist.");
		}
	}
}