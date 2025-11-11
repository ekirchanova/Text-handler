using System;
using System.IO;
using System.Linq;
using System.Threading;
using textHandlerClass;

namespace textHandlerTest
{
	[TestClass]
	public sealed class TextHandlerFileProcessingTests
	{
		private string GetTempFilePath(string prefix = "test")
		{
			return Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid()}.txt");
		}
		
		[TestCleanup]
		public void Cleanup()
		{
			var tempFiles = Directory.GetFiles(Path.GetTempPath(), "test_*.txt");
			foreach (var file in tempFiles)
			{
				try
				{
					if (File.Exists(file))
						File.Delete(file);
				}
				catch
				{
					
				}
			}
		}
		
		[TestMethod]
		public void ProcessSmallFile_ShouldFilterWordsByMinLength()
		{
			var inputFile = GetTempFilePath("small_input");
			var outputFile = GetTempFilePath("small_output");
			
			TestFileGenerator.CreateSmallTestFile(inputFile);
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };
			
			var progress = new Progress<int>();
			handler.ProcessFiles(progress, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
			var outputLines = File.ReadAllLines(outputFile);
			Assert.IsTrue(outputLines.Length > 0);
			
			var allWords = string.Join(" ", outputLines).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.IsTrue(allWords.All(word => word.Length >= 3), "Все слова должны быть длиной >= 3");
		}
		
		[TestMethod]
		public void ProcessMediumFile_1KB_ShouldProcessCorrectly()
		{
			var inputFile = GetTempFilePath("medium_input");
			var outputFile = GetTempFilePath("medium_output");
			
			TestFileGenerator.CreateTextFile(inputFile, targetSizeBytes: 1024, randomSeed: 42);
			
			var handler = new TextHandler(minAmountOfSymbols: 5, needDeletePunctuationMarks: true);
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };


            var progress = new TestProgress<int>();
            var progressValues = new List<int>();
            progress.OnReport += value => progressValues.Add(value);

            handler.ProcessFiles(progress, CancellationToken.None);
			Assert.IsNotEmpty(progressValues);
			
			Assert.IsTrue(File.Exists(outputFile));
			var outputLines = File.ReadAllLines(outputFile);
			
			var allText = string.Join("", outputLines);
			Assert.IsFalse(allText.Any(c => char.IsPunctuation(c)), "Знаки препинания должны быть удалены");
			
			var words = allText.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			if (words.Length > 0)
			{
				Assert.IsTrue(words.All(w => w.Length >= 5), "Все слова должны быть длиной >= 5");
			}
		}
		
		[TestMethod]
		public void ProcessLargeFile_100KB_ShouldProcessCorrectly()
		{
			var inputFile = GetTempFilePath("large_input");
			var outputFile = GetTempFilePath("large_output");
			
			TestFileGenerator.CreateTextFile(inputFile, targetSizeBytes: 100 * 1024, randomSeed: 123);
			
			var handler = new TextHandler(minAmountOfSymbols: 4, needDeletePunctuationMarks: false);
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };


            var progress = new TestProgress<int>();
            var progressValues = new List<int>();
            progress.OnReport += value => progressValues.Add(value);

            handler.ProcessFiles(progress, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
			var outputFileInfo = new FileInfo(outputFile);
			
			Assert.IsTrue(outputFileInfo.Length >= 0);
		}
		
		[TestMethod]
		public void ProcessVeryLargeFile_1MB_ShouldComplete()
		{
			var inputFile = GetTempFilePath("verylarge_input");
			var outputFile = GetTempFilePath("verylarge_output");
			
			TestFileGenerator.CreateTextFile(inputFile, targetSizeBytes: 1024 * 1024, randomSeed: 456);
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };
			
			var progress = new Progress<int>();
			handler.ProcessFiles(progress, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
		}
		
		[TestMethod]
		public void ProcessMultipleFiles_DifferentSizes_ShouldProcessAll()
		{
			var inputFiles = new[]
			{
				GetTempFilePath("multi1_input"),
				GetTempFilePath("multi2_input"),
				GetTempFilePath("multi3_input")
			};
			
			var outputFiles = new[]
			{
				GetTempFilePath("multi1_output"),
				GetTempFilePath("multi2_output"),
				GetTempFilePath("multi3_output")
			};
			
			TestFileGenerator.CreateTextFile(inputFiles[0], targetSizeBytes: 512, randomSeed: 1);
			TestFileGenerator.CreateTextFile(inputFiles[1], targetSizeBytes: 2048, randomSeed: 2);
			TestFileGenerator.CreateTextFile(inputFiles[2], targetSizeBytes: 10240, randomSeed: 3);
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			handler.InputFiles = inputFiles;
			handler.OutputFiles = outputFiles;
			
			var progressValues = new System.Collections.Concurrent.ConcurrentBag<int>();
			var progress = new Progress<int>(v => progressValues.Add(v));
			handler.ProcessFiles(progress, CancellationToken.None);
			
			foreach (var outputFile in outputFiles)
			{
				Assert.IsTrue(File.Exists(outputFile), $"Output file {outputFile} should exist");
			}
			
			Assert.IsNotEmpty(progressValues, "Progress should be reported");
		}
		
		[TestMethod]
		public void ProcessEmptyLinesFile_ShouldHandleCorrectly()
		{
			var inputFile = GetTempFilePath("empty_input");
			var outputFile = GetTempFilePath("empty_output");
			
			TestFileGenerator.CreateEmptyLinesFile(inputFile, lineCount: 100);
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };
			
			var progress = new Progress<int>();
			handler.ProcessFiles(progress, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
			var outputLines = File.ReadAllLines(outputFile);
			Assert.IsTrue(outputLines.All(line => string.IsNullOrWhiteSpace(line)));
		}
		
		[TestMethod]
		public void ProcessWhitespaceFile_ShouldFilterCorrectly()
		{
			var inputFile = GetTempFilePath("whitespace_input");
			var outputFile = GetTempFilePath("whitespace_output");
			
			TestFileGenerator.CreateWhitespaceFile(inputFile, targetSizeBytes: 500);
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };
			
			var progress = new Progress<int>();
			handler.ProcessFiles(progress, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
			var outputLines = File.ReadAllLines(outputFile);
			Assert.IsTrue(outputLines.All(line => string.IsNullOrWhiteSpace(line)));
		}
		
		[TestMethod]
		public void ProcessFileWithCancellation_ShouldHandleGracefully()
		{
			var inputFile = GetTempFilePath("cancel_input");
			var outputFile = GetTempFilePath("cancel_output");
			
			TestFileGenerator.CreateTextFile(inputFile, targetSizeBytes: 50 * 1024, randomSeed: 789);
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };
			
			using var cts = new CancellationTokenSource();
			var progress = new Progress<int>();
			
			cts.Cancel();
			
			Assert.Throws<OperationCanceledException>(() =>
			{
				handler.ProcessFiles(progress, cts.Token);
			});
		}
		
		[TestMethod]
		public void ProcessFileByLines_ShouldMaintainLineCount()
		{
			var inputFile = GetTempFilePath("lines_input");
			var outputFile = GetTempFilePath("lines_output");
			
			const int lineCount = 1000;
			TestFileGenerator.CreateTextFileByLines(inputFile, lineCount, wordsPerLine: 5, randomSeed: 999);
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };
			
			var progress = new Progress<int>();
			handler.ProcessFiles(progress, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
			var inputLines = File.ReadAllLines(inputFile);
			var outputLines = File.ReadAllLines(outputFile);
			
			Assert.AreEqual(inputLines.Length, outputLines.Length, "Line count should match");
		}
	}

}
