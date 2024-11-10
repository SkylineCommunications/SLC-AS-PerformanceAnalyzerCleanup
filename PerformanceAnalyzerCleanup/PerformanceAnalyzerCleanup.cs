namespace Skyline.DataMiner.Utilities.PerformanceAnalyzerCleanup
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	using Skyline.DataMiner.Automation;

	public class Script
	{
		private string performanceMetricsLocation;
		private DateTime maxDaysSinceLastModified;
		private HashSet<string> fileNamesToDelete;
		private bool hasFailures;
		private IEngine engine;

		public void Run(IEngine engine)
		{
			try
			{
				this.engine = engine;
				RunSafe();
			}
			catch (Exception ex)
			{
				engine.ExitFail("Something went wrong: " + ex.Message);
			}

			if (hasFailures)
			{
				engine.ExitFail("Failed to delete some files. Check SLAutomation logging.");
			}
		}

		private void RunSafe()
		{
			Initialize();

			if (!Directory.Exists(performanceMetricsLocation))
			{
				throw new DirectoryNotFoundException("The directory does not exist.");
			}

			DetermineFilesToDelete();
			DeleteFiles();
		}

		private void Initialize()
		{
			maxDaysSinceLastModified = GetOldestPerformanceDate();
			performanceMetricsLocation = GetFolderPath();
			fileNamesToDelete = new HashSet<string>();
		}

		private void DetermineFilesToDelete()
		{
			string[] files = Directory.GetFiles(performanceMetricsLocation);

			foreach (string file in files)
			{
				if (File.GetLastWriteTime(file) < maxDaysSinceLastModified)
				{
					fileNamesToDelete.Add(file);
				}
			}
		}

		private void DeleteFiles()
		{
			foreach (string fileName in fileNamesToDelete)
			{
				try
				{
					File.Delete(fileName);
				}
				catch (Exception ex)
				{
					hasFailures = true;
					engine.Log($"Failed to delete file: {fileName} - {ex.Message}");
				}
			}
		}

		private DateTime GetOldestPerformanceDate()
		{
			var inputOfDays = engine.GetScriptParam("Max Days Since Last Modified")?.Value;
			if (string.IsNullOrEmpty(inputOfDays) || !int.TryParse(inputOfDays, out int days))
			{
				throw new ArgumentException("Invalid or missing value for Days of oldest performance info. It must be a valid integer.");
			}

			return DateTime.Now.AddDays(-days);
		}

		private string GetFolderPath()
		{
			var inputOfFolderPath = Convert.ToString(engine.GetScriptParam("Performance Metrics Location")?.Value);
			if (string.IsNullOrEmpty(inputOfFolderPath))
			{
				throw new ArgumentException("Missing value for Folder path to performance info.");
			}

			return inputOfFolderPath.Trim();
		}
	}
}