using System;
using System.IO;
using System.Linq;
using System.Threading;
using textHandlerClass;

namespace textHandlerTest
{
	[TestClass]
	public sealed class TextHandlerIntegrationTests
	{
		private string GetTempFilePath(string prefix = "test")
		{
			return Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid()}.txt");
		}
		
		[TestCleanup]
		public void Cleanup()
		{
			var tempFiles = Directory.GetFiles(Path.GetTempPath(), "integration_test_*.txt");
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
		public void FullWorkflow_CheckPossibility_ProcessFiles_ShouldWork()
		{
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			var inputFile = GetTempFilePath("integration_test_input");
			var outputFile = GetTempFilePath("integration_test_output");
			
			File.WriteAllLines(inputFile, new[]
			{
				"Hello world this is a test",
				"Short words like a and I",
				"Longer words like programming"
			});
			
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };
			
			bool canProcess = handler.CheckFileProcessPosibility();
			Assert.IsTrue(canProcess, "Should be able to process files");
			
			handler.ProcessFiles(null, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(3, outputLines.Length);
			
			var allWords = string.Join(" ", outputLines).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.IsTrue(allWords.All(word => word.Length >= 3));
		}
		
		[TestMethod]
		public void FullWorkflow_WithPunctuationDeletion_ShouldWork()
		{
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: true);
			var inputFile = GetTempFilePath("integration_test_punct_input");
			var outputFile = GetTempFilePath("integration_test_punct_output");
			
			File.WriteAllLines(inputFile, new[]
			{
				"Hello, world!",
				"How are you?",
				"I'm fine, thanks!"
			});
			
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };
			
			handler.ProcessFiles(null, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
			var outputLines = File.ReadAllLines(outputFile);
			var allText = string.Join("", outputLines);
			Assert.IsFalse(allText.Any(c => char.IsPunctuation(c)));
		}
		
		[TestMethod]
		public void FullWorkflow_MultipleFiles_WithProgress_ShouldWork()
		{
			var handler = new TextHandler(minAmountOfSymbols: 4, needDeletePunctuationMarks: false);
			var inputFiles = new[]
			{
				GetTempFilePath("integration_test_multi_input1"),
				GetTempFilePath("integration_test_multi_input2"),
				GetTempFilePath("integration_test_multi_input3")
			};
			var outputFiles = new[]
			{
				GetTempFilePath("integration_test_multi_output1"),
				GetTempFilePath("integration_test_multi_output2"),
				GetTempFilePath("integration_test_multi_output3")
			};
			
			for (int i = 0; i < inputFiles.Length; i++)
			{
				File.WriteAllText(inputFiles[i], $"File {i} content with words");
			}
			
			handler.InputFiles = inputFiles;
			handler.OutputFiles = outputFiles;

            var progress = new TestProgress<int>();
            var progressValues = new List<int>();
            progress.OnReport += value => progressValues.Add(value);

            handler.ProcessFiles(progress, CancellationToken.None);
			
			foreach (var outputFile in outputFiles)
			{
				Assert.IsTrue(File.Exists(outputFile));
			}

            Assert.IsNotEmpty(progressValues);
			Assert.IsTrue(progressValues.Contains(100));
		}
		
		[TestMethod]
		public void FullWorkflow_ChangeSettings_ProcessFiles_ShouldUseNewSettings()
		{
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			var inputFile = GetTempFilePath("integration_test_change_input");
			var outputFile = GetTempFilePath("integration_test_change_output");
			
			File.WriteAllLines(inputFile, new[] { "a ab abc abcd abcde" });
			
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };
			
			handler.ProcessFiles(null, CancellationToken.None);
			var output1 = File.ReadAllLines(outputFile);
			var words1 = output1[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			
			handler.MinAmountOfSymbols = 5;
			var outputFile2 = GetTempFilePath("integration_test_change_output2");
			handler.OutputFiles = new[] { outputFile2 };
			handler.ProcessFiles(null, CancellationToken.None);
			var output2 = File.ReadAllLines(outputFile2);
			var words2 = output2[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			
			Assert.IsTrue(words1.Length > words2.Length, "With minSymbols=5 should filter more words");
		}
		
		[TestMethod]
		public void FullWorkflow_EmptyFile_ShouldCreateEmptyOutput()
		{
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			var inputFile = GetTempFilePath("integration_test_empty_input");
			var outputFile = GetTempFilePath("integration_test_empty_output");
			
			File.WriteAllText(inputFile, string.Empty);
			
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };
			
			handler.ProcessFiles(null, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(0, outputLines.Length);
		}
		
		[TestMethod]
		public void FullWorkflow_LargeDataset_ShouldCompleteSuccessfully()
		{
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			var inputFile = GetTempFilePath("integration_test_large_input");
			var outputFile = GetTempFilePath("integration_test_large_output");
			
			var lines = new string[10000];
			for (int i = 0; i < lines.Length; i++)
			{
				lines[i] = $"Line {i} with some content for testing";
			}
			File.WriteAllLines(inputFile, lines);
			
			handler.InputFiles = new[] { inputFile };
			handler.OutputFiles = new[] { outputFile };
			
			handler.ProcessFiles(null, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(10000, outputLines.Length);
		}
		
		[TestMethod]
		public void FullWorkflow_HandleFilesMas_DirectCall_ShouldWork()
		{
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			var inputFiles = new[]
			{
				GetTempFilePath("integration_test_direct_input1"),
				GetTempFilePath("integration_test_direct_input2")
			};
			var outputFiles = new[]
			{
				GetTempFilePath("integration_test_direct_output1"),
				GetTempFilePath("integration_test_direct_output2")
			};
			
			File.WriteAllText(inputFiles[0], "first file content");
			File.WriteAllText(inputFiles[1], "second file content");
			
			handler.HandleFilesMas(inputFiles, outputFiles, null, CancellationToken.None);
			
			foreach (var outputFile in outputFiles)
			{
				Assert.IsTrue(File.Exists(outputFile));
			}
		}
		
		[TestMethod]
		public void FullWorkflow_HandleSingleFile_DirectCall_ShouldWork()
		{
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			var inputFile = GetTempFilePath("integration_test_single_input");
			var outputFile = GetTempFilePath("integration_test_single_output");
			
			File.WriteAllText(inputFile, "test content for single file processing");
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
		}
	}

}
