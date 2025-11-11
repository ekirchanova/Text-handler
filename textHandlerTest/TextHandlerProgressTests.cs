using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using textHandlerClass;

namespace textHandlerTest
{
	[TestClass]
	public sealed class TextHandlerProgressTests
	{
		private string GetTempFilePath(string prefix = "test")
		{
			return Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid()}.txt");
		}

		[TestCleanup]
		public void Cleanup()
		{
			var tempFiles = Directory.GetFiles(Path.GetTempPath(), "progress_test_*.txt");
			foreach (var file in tempFiles)
			{
				try
				{
					if (File.Exists(file))
						File.Delete(file);
				}
				catch { }
			}
		}

		[TestMethod]
		public void HandleFilesMas_WithConcurrentBag_ShouldCollectAllProgressValues()
		{
			var handler = new TextHandler();
			var inputFiles = new[]
			{
				GetTempFilePath("progress_test_input1"),
				GetTempFilePath("progress_test_input2"),
				GetTempFilePath("progress_test_input3")
			};
			var outputFiles = new[]
			{
				GetTempFilePath("progress_test_output1"),
				GetTempFilePath("progress_test_output2"),
				GetTempFilePath("progress_test_output3")
			};

			foreach (var file in inputFiles)
			{
				File.WriteAllText(file, "test content");
			}

            var progress = new TestProgress<int>();
            var progressValues = new List<int>();
            progress.OnReport += value => progressValues.Add(value);

            handler.HandleFilesMas(inputFiles, outputFiles, progress, CancellationToken.None);

            Assert.IsNotEmpty(progressValues, "Progress should be reported");

			Assert.IsTrue(progressValues.Contains(100), "Should reach 100%");
			
			Assert.IsTrue(progressValues.Any(v => v > 0 && v < 100), "Should have intermediate progress values");
		}

		[TestMethod]
		public void HandleFilesMas_WithLastValueTracking_ShouldTrackFinalProgress()
		{
			var handler = new TextHandler();
			var inputFiles = new[]
			{
				GetTempFilePath("progress_test_last_input1"),
				GetTempFilePath("progress_test_last_input2")
			};
			var outputFiles = new[]
			{
				GetTempFilePath("progress_test_last_output1"),
				GetTempFilePath("progress_test_last_output2")
			};

			foreach (var file in inputFiles)
			{
				File.WriteAllText(file, "test content");
			}


            var progress = new TestProgress<int>();
            var progressValues = new List<int>();
            progress.OnReport += value => progressValues.Add(value);

            handler.HandleFilesMas(inputFiles, outputFiles, progress, CancellationToken.None);

            Assert.IsNotEmpty(progressValues, "Progress should be reported");
			var lastProgress = progressValues.Last();
            Assert.AreEqual(100, lastProgress, "Final progress should be 100%");
		}

		[TestMethod]
		public void HandleFilesMas_WithSingleFile_ShouldReport100Percent()
		{
			var handler = new TextHandler();
			var inputFile = GetTempFilePath("progress_test_single_input");
			var outputFile = GetTempFilePath("progress_test_single_output");

			File.WriteAllText(inputFile, "test content");

            var progress = new TestProgress<int>();
            var progressValues = new List<int>();
            progress.OnReport += value => progressValues.Add(value);

            handler.HandleFilesMas(
				new[] { inputFile },
				new[] { outputFile },
				progress,
				CancellationToken.None
			);

            Assert.IsNotEmpty(progressValues, "Progress should be reported");
            Assert.IsTrue(progressValues.Contains(100), "Should report 100% for single file");
		}

		[TestMethod]
		public void HandleFilesMas_WithManyFiles_ShouldReportProgressForEach()
		{
			var handler = new TextHandler();
			int fileCount = 10;
			var inputFiles = new string[fileCount];
			var outputFiles = new string[fileCount];

			for (int i = 0; i < fileCount; i++)
			{
				inputFiles[i] = GetTempFilePath($"progress_test_many_input_{i}");
				outputFiles[i] = GetTempFilePath($"progress_test_many_output_{i}");
				File.WriteAllText(inputFiles[i], $"content {i}");
			}


            var progress = new TestProgress<int>();
            var progressValues = new List<int>();
            progress.OnReport += value => progressValues.Add(value);

            handler.HandleFilesMas(inputFiles, outputFiles, progress, CancellationToken.None);

            Assert.IsNotEmpty(progressValues, "Progress should be reported");

            var progressList = progressValues.ToList();
			Assert.IsTrue(progressList.Contains(100), "Should reach 100%");
			
			var maxProgress = progressList.Max();
			Assert.AreEqual(100, maxProgress, "Maximum progress should be 100%");
		}

		[TestMethod]
		public void HandleFilesMas_WithNullProgress_ShouldNotThrow()
		{
			var handler = new TextHandler();
			var inputFile = GetTempFilePath("progress_test_null_input");
			var outputFile = GetTempFilePath("progress_test_null_output");

			File.WriteAllText(inputFile, "test content");

			handler.HandleFilesMas(
				new[] { inputFile },
				new[] { outputFile },
				null,
				CancellationToken.None
			);

			Assert.IsTrue(File.Exists(outputFile));
		}

		[TestMethod]
		public void HandleFilesMas_ProgressValues_ShouldBeInValidRange()
		{
			var handler = new TextHandler();
			var inputFiles = new[]
			{
				GetTempFilePath("progress_test_range_input1"),
				GetTempFilePath("progress_test_range_input2"),
				GetTempFilePath("progress_test_range_input3"),
				GetTempFilePath("progress_test_range_input4"),
				GetTempFilePath("progress_test_range_input5")
			};
			var outputFiles = inputFiles.Select(f => f.Replace("input", "output")).ToArray();

			foreach (var file in inputFiles)
			{
				File.WriteAllText(file, "test content");
			}

            var progress = new TestProgress<int>();
            var progressValues = new List<int>();
            progress.OnReport += value => progressValues.Add(value);

            handler.HandleFilesMas(inputFiles, outputFiles, progress, CancellationToken.None);

			var progressList = progressValues.ToList();

            Assert.IsNotEmpty(progressValues, "Progress should be reported");
            Assert.IsTrue(progressList.All(v => v >= 0 && v <= 100), 
				"All progress values should be in range [0, 100]");
			
			Assert.IsTrue(progressList.Any(v => v == 100), 
				"Should have progress value of 100%");
		}

		[TestMethod]
		public void HandleFilesMas_WithProgressHelper_ShouldWorkCorrectly()
		{
			// Arrange
			var handler = new TextHandler();
			var inputFiles = new[]
			{
				GetTempFilePath("progress_test_helper_input1"),
				GetTempFilePath("progress_test_helper_input2")
			};
			var outputFiles = new[]
			{
				GetTempFilePath("progress_test_helper_output1"),
				GetTempFilePath("progress_test_helper_output2")
			};

			foreach (var file in inputFiles)
			{
				File.WriteAllText(file, "test content");
			}

            var progress = new TestProgress<int>();
            var progressValues = new List<int>();
            progress.OnReport += value => progressValues.Add(value);

            handler.HandleFilesMas(inputFiles, outputFiles, progress, CancellationToken.None);

            Assert.IsNotEmpty(progressValues, "Progress should be reported");
            Assert.IsTrue(progressValues.Contains(100), 
				"Should reach 100% progress");
		}

		[TestMethod]
		public void ProcessFiles_WithProgress_ShouldDelegateToHandleFilesMas()
		{
			var handler = new TextHandler();
			var inputFile = GetTempFilePath("progress_test_process_input");
			var outputFile = GetTempFilePath("progress_test_process_output");

			File.WriteAllText(inputFile, "test content");

			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };

            var progress = new TestProgress<int>();
            var progressValues = new List<int>();
            progress.OnReport += value => progressValues.Add(value);

            handler.ProcessFiles(progress, CancellationToken.None);

			Assert.IsNotEmpty(progressValues, "Progress should be reported");
			Assert.IsTrue(progressValues.Contains(100), "Should reach 100%");
			Assert.IsTrue(File.Exists(outputFile));
		}
	}

}
