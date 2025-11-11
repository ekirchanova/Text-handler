using System;
using System.IO;
using System.Linq;
using System.Threading;
using textHandlerClass;

namespace textHandlerTest
{
	[TestClass]
	public sealed class TextHandlerEdgeCasesTests
	{
		private string GetTempFilePath(string prefix = "test")
		{
			return Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid()}.txt");
		}
		
		[TestCleanup]
		public void Cleanup()
		{
			var tempFiles = Directory.GetFiles(Path.GetTempPath(), "edge_test_*.txt");
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
		public void HandleSingleFile_WithOnlyPunctuation_DeleteEnabled_ShouldReturnEmpty()
		{
			var inputFile = GetTempFilePath("edge_test_punct_only_input");
			var outputFile = GetTempFilePath("edge_test_punct_only_output");
			File.WriteAllLines(inputFile, new[] { "!!!", "???", ",,," });
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: true);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(3, outputLines.Length);
			Assert.IsTrue(outputLines.All(line => string.IsNullOrWhiteSpace(line)));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithUnicodeCharacters_ShouldHandleCorrectly()
		{
			var inputFile = GetTempFilePath("edge_test_unicode_input");
			var outputFile = GetTempFilePath("edge_test_unicode_output");
			File.WriteAllLines(inputFile, new[] { "Привет мир", "Hello 世界", "مرحبا" });
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(3, outputLines.Length);
			Assert.IsTrue(outputLines.Contains("Привет мир"));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithVeryLongLine_ShouldHandleCorrectly()
		{
			var inputFile = GetTempFilePath("edge_test_long_line_input");
			var outputFile = GetTempFilePath("edge_test_long_line_output");
			string veryLongLine = string.Join(" ", Enumerable.Range(0, 10000).Select(i => $"word{i}"));
			File.WriteAllText(inputFile, veryLongLine);
			
			var handler = new TextHandler(minAmountOfSymbols: 4, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var words = outputLines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.IsTrue(words.All(w => w.Length >= 4));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithRepeatedWords_ShouldHandleCorrectly()
		{
			var inputFile = GetTempFilePath("edge_test_repeated_input");
			var outputFile = GetTempFilePath("edge_test_repeated_output");
			File.WriteAllLines(inputFile, new[] { "test test test test" });
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var words = outputLines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.AreEqual(4, words.Length); 
			Assert.IsTrue(words.All(w => w == "test"));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithNumbersOnly_ShouldHandleCorrectly()
		{
			var inputFile = GetTempFilePath("edge_test_numbers_input");
			var outputFile = GetTempFilePath("edge_test_numbers_output");
			File.WriteAllLines(inputFile, new[] { "123 4567 89 12345" });
			
			var handler = new TextHandler(minAmountOfSymbols: 4, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var words = outputLines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.IsTrue(words.Contains("4567"));
			Assert.IsTrue(words.Contains("12345"));
			Assert.IsFalse(words.Contains("123"));
			Assert.IsFalse(words.Contains("89"));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithMixedCase_ShouldHandleCorrectly()
		{
			var inputFile = GetTempFilePath("edge_test_case_input");
			var outputFile = GetTempFilePath("edge_test_case_output");
			File.WriteAllLines(inputFile, new[] { "Hello WORLD test" });
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var words = outputLines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.IsTrue(words.Contains("Hello"));
			Assert.IsTrue(words.Contains("WORLD"));
			Assert.IsTrue(words.Contains("test"));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithSingleCharacterWords_ShouldFilterBasedOnMinLength()
		{
			var inputFile = GetTempFilePath("edge_test_single_char_input");
			var outputFile = GetTempFilePath("edge_test_single_char_output");
			File.WriteAllLines(inputFile, new[] { "a b c d e f" });
			
			var handler = new TextHandler(minAmountOfSymbols: 1, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var words = outputLines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.AreEqual(6, words.Length); 
		}
		
		[TestMethod]
		public void HandleSingleFile_WithSingleCharacterWords_MinLength2_ShouldFilterAll()
		{
			var inputFile = GetTempFilePath("edge_test_single_char_filter_input");
			var outputFile = GetTempFilePath("edge_test_single_char_filter_output");
			File.WriteAllLines(inputFile, new[] { "a b c d e f" });
			
			var handler = new TextHandler(minAmountOfSymbols: 2, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var words = outputLines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.AreEqual(0, words.Length); 
		}
		
		[TestMethod]
		public void HandleSingleFile_WithNewlinesOnly_ShouldHandleCorrectly()
		{
			var inputFile = GetTempFilePath("edge_test_newlines_input");
			var outputFile = GetTempFilePath("edge_test_newlines_output");
			File.WriteAllText(inputFile, "\n\n\n");
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(3, outputLines.Length);
			Assert.IsTrue(outputLines.All(line => string.IsNullOrWhiteSpace(line)));
		}
		
		[TestMethod]
		public void HandleFilesMas_WithSingleFile_ShouldProcessCorrectly()
		{
			var handler = new TextHandler();
			var inputFile = GetTempFilePath("edge_test_single_file_input");
			var outputFile = GetTempFilePath("edge_test_single_file_output");
			
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
		public void HandleFilesMas_WithManyFiles_ShouldProcessAll()
		{
			var handler = new TextHandler();
			int fileCount = 10;
			var inputFiles = new string[fileCount];
			var outputFiles = new string[fileCount];
			
			for (int i = 0; i < fileCount; i++)
			{
				inputFiles[i] = GetTempFilePath($"edge_test_many_input_{i}");
				outputFiles[i] = GetTempFilePath($"edge_test_many_output_{i}");
				File.WriteAllText(inputFiles[i], $"content {i}");
			}
			
			handler.HandleFilesMas(inputFiles, outputFiles, null, CancellationToken.None);
			
			for (int i = 0; i < fileCount; i++)
			{
				Assert.IsTrue(File.Exists(outputFiles[i]), $"Output file {i} should exist");
			}
		}
		
		[TestMethod]
		public void ProcessFiles_WithDifferentMinAmountOfSymbols_ShouldFilterCorrectly()
		{
			var inputFile = GetTempFilePath("edge_test_min_symbols_input");
			var outputFile = GetTempFilePath("edge_test_min_symbols_output");
			File.WriteAllLines(inputFile, new[] { "a ab abc abcd abcde" });
			
			for (uint minSymbols = 0; minSymbols <= 5; minSymbols++)
			{
				var handler = new TextHandler(minAmountOfSymbols: minSymbols, needDeletePunctuationMarks: false);
				handler.InputFiles = new[] { inputFile };
				handler.OutputFiles = new[] { outputFile };
				
				handler.ProcessFiles(null, CancellationToken.None);
				
				var outputLines = File.ReadAllLines(outputFile);
				Assert.AreEqual(1, outputLines.Length);
				var words = outputLines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				
				if (words.Length > 0)
				{
					Assert.IsTrue(words.All(w => w.Length >= minSymbols),
						$"All words should have length >= {minSymbols}");
				}
			}
		}
		
		[TestMethod]
		public void HandleSingleFile_WithConcurrentAccess_ShouldHandleCorrectly()
		{
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			var inputFile = GetTempFilePath("edge_test_concurrent_input");
			var outputFile1 = GetTempFilePath("edge_test_concurrent_output1");
			var outputFile2 = GetTempFilePath("edge_test_concurrent_output2");
			
			File.WriteAllText(inputFile, "test content for concurrent access");
			
			handler.HandleSingleFile(inputFile, outputFile1, CancellationToken.None);
			handler.HandleSingleFile(inputFile, outputFile2, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile1));
			Assert.IsTrue(File.Exists(outputFile2));
			var lines1 = File.ReadAllLines(outputFile1);
			var lines2 = File.ReadAllLines(outputFile2);
			Assert.AreEqual(lines1.Length, lines2.Length);
		}
	}

}
