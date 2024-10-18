namespace PerformanceLoggerCleanupScript_1.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq.Expressions;
    using Moq;
    using Skyline.DataMiner.Automation;
    using static Skyline.DataMiner.Net.Tools;

    [TestClass]
    public class PerformanceCleanupScriptTests
    {
        private Mock<IEngine> mockEngine;
        private Script script;
        private const string TestFolderPath = @"C:\Skyline_Data\PerformanceLogger"; // Ensure this path is suitable for testing.

        [TestInitialize]
        public void Setup()
        {
            mockEngine = new Mock<IEngine>();
            script = new Script();
        }

        [TestMethod]
        public void Run_DirectoryNotFound_ExitsWithMessage()
        {
            var mockScriptParam = new Mock<ScriptParam>();
            mockScriptParam.Setup(sp => sp.Value).Returns("7");

            mockEngine.Setup(e => e.GetScriptParam(It.IsAny<string>()))
                       .Returns(mockScriptParam.Object);

            if (Directory.Exists(TestFolderPath))
            {
                Directory.Delete(TestFolderPath, true);
            }

            script.Run(mockEngine.Object);

            mockEngine.Verify(e => e.ExitFail(It.Is<string>(s => s.Contains("Directory not found"))), Times.Once);
        }

        [TestMethod]
        public void Run_AccessDenied_ExitsWithMessage()
        {
            var mockScriptParam = new Mock<ScriptParam>();
            mockScriptParam.Setup(sp => sp.Value).Returns("7");

            mockEngine.Setup(e => e.GetScriptParam(It.IsAny<string>())).Throws(new UnauthorizedAccessException("Simulated access denied error."));
            script.Run(mockEngine.Object);

            mockEngine.Verify(e => e.ExitFail(It.Is<string>(s => s.Contains("Access denied"))), Times.Once);
        }

        [TestMethod]
        public void Run_ValidParameters_DeletesOldFiles()
        {
            var mockScriptParam = new Mock<ScriptParam>();
            mockScriptParam.Setup(sp => sp.Value).Returns("7");
            mockEngine.Setup(e => e.GetScriptParam("Days of oldest performance info")).Returns(mockScriptParam.Object);

            Directory.CreateDirectory(TestFolderPath);
            File.WriteAllText(Path.Combine(TestFolderPath, "oldFile.txt"), "test content");
            File.SetLastWriteTime(Path.Combine(TestFolderPath, "oldFile.txt"), DateTime.Now.AddDays(-10));

            File.WriteAllText(Path.Combine(TestFolderPath, "newFile.txt"), "test content");
            File.SetLastWriteTime(Path.Combine(TestFolderPath, "newFile.txt"), DateTime.Now);

            script.Run(mockEngine.Object);

            Assert.IsFalse(File.Exists(Path.Combine(TestFolderPath, "oldFile.txt")), "Old file should be deleted.");
            Assert.IsTrue(File.Exists(Path.Combine(TestFolderPath, "newFile.txt")), "New file should still exist.");

            Directory.Delete(TestFolderPath, true);
        }

        [TestMethod]
        public void Initialize_InvalidDaysParameter_ThrowsArgumentException()
        {
            var mockScriptParam = new Mock<ScriptParam>();
            mockScriptParam.Setup(sp => sp.Value).Returns("invalid"); // Set to invalid input
            mockEngine.Setup(e => e.GetScriptParam("Days of oldest performance info")).Returns(mockScriptParam.Object);

            var exception = Assert.ThrowsException<ArgumentException>(() =>
            {
                script.Initialize(mockEngine.Object);
            });

            Assert.AreEqual("Invalid or missing value for Days of oldest performance info. It must be a valid integer.", exception.Message);
        }
    }
}