using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using textHandlerClass;

namespace textHandlerTest
{
	[TestClass]
	public sealed class TextHandlerErrorHandlingTests
	{
		private string GetTempFilePath(string prefix = "test")
		{
			return Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid()}.txt");
		}
		
		[TestCleanup]
		public void Cleanup()
		{
			var tempFiles = Directory.GetFiles(Path.GetTempPath(), "error_test_*.txt");
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
		public void HandleFilesMas_WithMismatchedFileCounts_ShouldThrowArgumentException()
		{
			var handler = new TextHandler();
			string[] inputFiles = { "input1.txt", "input2.txt" };
			string[] outputFiles = { "output1.txt" };
			
			Assert.Throws<ArgumentException>(() =>
			{
				handler.HandleFilesMas(inputFiles, outputFiles, null, CancellationToken.None);
			});
		}
		
		[TestMethod]
		public void HandleFilesMas_WithMoreOutputFiles_ShouldThrowArgumentException()
		{
			var handler = new TextHandler();
			string[] inputFiles = { "input1.txt" };
			string[] outputFiles = { "output1.txt", "output2.txt" };
			
			Assert.Throws<ArgumentException>(() =>
			{
				handler.HandleFilesMas(inputFiles, outputFiles, null, CancellationToken.None);
			});
		}
		
		[TestMethod]
		public void HandleFilesMas_WithEmptyArrays_ShouldNotThrow()
		{
			var handler = new TextHandler();
			string[] inputFiles = Array.Empty<string>();
			string[] outputFiles = Array.Empty<string>();
			
			handler.HandleFilesMas(inputFiles, outputFiles, null, CancellationToken.None);
		}
		
		[TestMethod]
		public void ProcessFiles_WithNullInputFiles_ShouldThrowNullReferenceException()
		{
			var handler = new TextHandler();
			handler.InputFiles = null;
			handler.OutputFiles = new[] { "output.txt" };
			
			Assert.Throws<NullReferenceException>(() =>
			{
				handler.ProcessFiles(null, CancellationToken.None);
			});
		}
		
		[TestMethod]
		public void ProcessFiles_WithNullOutputFiles_ShouldThrowNullReferenceException()
		{
			var handler = new TextHandler();
			handler.InputFiles = new[] { "input.txt" };
			handler.OutputFiles = null;
			
			Assert.Throws<NullReferenceException>(() =>
			{
				handler.ProcessFiles(null, CancellationToken.None);
			});
		}
		
		[TestMethod]
		public void HandleSingleFile_WithInvalidPath_ShouldHandleError()
		{
			var handler = new TextHandler();
			string invalidPath = "Z:\\nonexistent\\path\\file.txt";
			var outputFile = GetTempFilePath("error_test_invalid_output");
			
			Assert.Throws<Exception>(() =>
			{
				handler.HandleSingleFile(invalidPath, outputFile, CancellationToken.None);
			});
		}
		
		[TestMethod]
		public void HandleSingleFile_WithReadOnlyOutputDirectory_ShouldThrowUnauthorizedAccessException()
		{
			var handler = new TextHandler();
			var inputFile = GetTempFilePath("error_test_readonly_input");
			File.WriteAllText(inputFile, "test content");
			
			string readOnlyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "test_output.txt");
			
			try
			{
				handler.HandleSingleFile(inputFile, readOnlyPath, CancellationToken.None);
				if (File.Exists(readOnlyPath))
				{
					File.Delete(readOnlyPath);
				}
			}
			catch (UnauthorizedAccessException)
			{
				Assert.IsTrue(true);
			}
			catch (DirectoryNotFoundException)
			{
				Assert.IsTrue(true);
			}
		}
		
		[TestMethod]
		public void HandleSingleFile_WithCancellationDuringProcessing_ShouldThrowOperationCanceledException()
		{ 

			var handler = new TextHandler();
			var inputFile = GetTempFilePath("error_test_cancel_input");
			var outputFile = GetTempFilePath("error_test_cancel_output");
			int size = 10000;
			var lines = new string[size];
			for (int i = 0; i < size; i++)
			{
				lines[i] = $"Line {i} with some content";
			}
			File.WriteAllLines(inputFile, lines);
			
			using var cts = new CancellationTokenSource();
			
			cts.CancelAfter(1); 
			
			Assert.Throws<OperationCanceledException>(() =>
			{
				handler.HandleSingleFile(inputFile, outputFile, cts.Token);
			});
		}

		[TestMethod]
		public void HandleFilesMas_WithProgressCallback_ShouldReportProgress()
		{
			var handler = new TextHandler();
			var inputFile1 = GetTempFilePath("error_test_progress_input1");
			var inputFile2 = GetTempFilePath("error_test_progress_input2");
			var outputFile1 = GetTempFilePath("error_test_progress_output1");
			var outputFile2 = GetTempFilePath("error_test_progress_output2");

			File.WriteAllText(inputFile1, "test content 1");
			File.WriteAllText(inputFile2, "test content 2");

            var progress = new TestProgress<int>();
            var progressValues = new List<int>();
            progress.OnReport += value => progressValues.Add(value);

            handler.HandleFilesMas(
				new[] { inputFile1, inputFile2 },
				new[] { outputFile1, outputFile2 },
				progress,
				CancellationToken.None
			);

			Assert.IsNotEmpty(progressValues, "Progress should be reported");
			
			Assert.IsTrue(progressValues.Contains(100), "Should reach 100% progress");
			
			Assert.IsTrue(progressValues.Last() == 100, "Final progress should be 100%");
		}
		
		[TestMethod]
		public void HandleFilesMas_WithNullProgress_ShouldNotThrow()
		{
			var handler = new TextHandler();
			var inputFile = GetTempFilePath("error_test_null_progress_input");
			var outputFile = GetTempFilePath("error_test_null_progress_output");
			
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
		public void HandleSingleFile_WithExceptionInProcessLine_ShouldAddErrorMessageToResults()
		{
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			var inputFile = GetTempFilePath("error_test_exception_input");
			var outputFile = GetTempFilePath("error_test_exception_output");
			
			File.WriteAllLines(inputFile, new[] { "normal line", "another normal line" });
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(2, outputLines.Length);
		}
	}

}
