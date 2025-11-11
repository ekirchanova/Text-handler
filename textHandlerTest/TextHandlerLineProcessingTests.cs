using System;
using System.IO;
using System.Threading;
using textHandlerClass;

namespace textHandlerTest
{
	[TestClass]
	public sealed class TextHandlerLineProcessingTests
	{
		private string GetTempFilePath(string prefix = "test")
		{
			return Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid()}.txt");
		}
		
		[TestCleanup]
		public void Cleanup()
		{
			var tempFiles = Directory.GetFiles(Path.GetTempPath(), "line_test_*.txt");
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
		public void HandleSingleFile_WithEmptyFile_ShouldCreateEmptyOutput()
		{
			var inputFile = GetTempFilePath("line_test_empty_input");
			var outputFile = GetTempFilePath("line_test_empty_output");
			File.WriteAllText(inputFile, string.Empty);
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			Assert.IsTrue(File.Exists(outputFile));
			var lines = File.ReadAllLines(outputFile);
			Assert.AreEqual(0, lines.Length);
		}
		
		[TestMethod]
		public void HandleSingleFile_WithOnlyWhitespace_ShouldReturnEmptyLines()
		{
			var inputFile = GetTempFilePath("line_test_whitespace_input");
			var outputFile = GetTempFilePath("line_test_whitespace_output");
			File.WriteAllLines(inputFile, new[] { "   ", "\t\t", "  \t  " });
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(3, outputLines.Length);
			Assert.IsTrue(outputLines.All(line => string.IsNullOrWhiteSpace(line)));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithMinAmountOfSymbols_Zero_ShouldKeepAllWords()
		{
			var inputFile = GetTempFilePath("line_test_zero_input");
			var outputFile = GetTempFilePath("line_test_zero_output");
			File.WriteAllLines(inputFile, new[] { "a ab abc abcd" });
			
			var handler = new TextHandler(minAmountOfSymbols: 0, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var words = outputLines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.AreEqual(4, words.Length); 
		}
		
		[TestMethod]
		public void HandleSingleFile_WithMinAmountOfSymbols_One_ShouldFilterCorrectly()
		{
			var inputFile = GetTempFilePath("line_test_one_input");
			var outputFile = GetTempFilePath("line_test_one_output");
			File.WriteAllLines(inputFile, new[] { "a ab abc abcd" });
			
			var handler = new TextHandler(minAmountOfSymbols: 1, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var words = outputLines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.AreEqual(4, words.Length); 
		}
		
		[TestMethod]
		public void HandleSingleFile_WithMinAmountOfSymbols_FiltersShortWords()
		{
			var inputFile = GetTempFilePath("line_test_filter_input");
			var outputFile = GetTempFilePath("line_test_filter_output");
			File.WriteAllLines(inputFile, new[] { "a ab abc abcd abcde" });
			
			var handler = new TextHandler(minAmountOfSymbols: 4, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var words = outputLines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.AreEqual(2, words.Length);
			Assert.IsTrue(words.Contains("abcd"));
			Assert.IsTrue(words.Contains("abcde"));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithPunctuationMarks_DeleteEnabled_ShouldRemovePunctuation()
		{
			var inputFile = GetTempFilePath("line_test_punct_input");
			var outputFile = GetTempFilePath("line_test_punct_output");
			File.WriteAllLines(inputFile, new[] { "Hello, world! How are you?" });
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: true);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			Assert.IsFalse(outputLines[0].Any(c => char.IsPunctuation(c)));
			Assert.IsTrue(outputLines[0].Contains("Hello"));
			Assert.IsTrue(outputLines[0].Contains("world"));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithPunctuationMarks_DeleteDisabled_ShouldKeepPunctuation()
		{
			var inputFile = GetTempFilePath("line_test_punct_keep_input");
			var outputFile = GetTempFilePath("line_test_punct_keep_output");
			File.WriteAllLines(inputFile, new[] { "Hello, world! How are you?" });
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var text = outputLines[0];
			Assert.IsFalse(string.IsNullOrWhiteSpace(text));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithTabDelimiters_ShouldHandleCorrectly()
		{
			var inputFile = GetTempFilePath("line_test_tab_input");
			var outputFile = GetTempFilePath("line_test_tab_output");
			File.WriteAllText(inputFile, "word1\tword2\tword3\twordsssss4");
			
			var handler = new TextHandler(minAmountOfSymbols: 5, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var words = outputLines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.IsTrue(words.All(w => w.Length >= 5));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithMixedDelimiters_ShouldHandleCorrectly()
		{
			var inputFile = GetTempFilePath("line_test_mixed_input");
			var outputFile = GetTempFilePath("line_test_mixed_output");
			File.WriteAllText(inputFile, "word1\t word2  word3\tword4");
			
			var handler = new TextHandler(minAmountOfSymbols: 5, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var words = outputLines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.IsTrue(words.All(w => w.Length >= 5));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithVeryLongWords_ShouldHandleCorrectly()
		{
			var inputFile = GetTempFilePath("line_test_long_input");
			var outputFile = GetTempFilePath("line_test_long_output");
			string longWord = new string('a', 1000);
			File.WriteAllLines(inputFile, new[] { $"short {longWord} medium" });
			
			var handler = new TextHandler(minAmountOfSymbols: 10, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			Assert.IsTrue(outputLines[0].Contains(longWord));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithMultipleLines_ShouldProcessAllLines()
		{
			var inputFile = GetTempFilePath("line_test_multiline_input");
			var outputFile = GetTempFilePath("line_test_multiline_output");
			File.WriteAllLines(inputFile, new[]
			{
				"line one with words",
				"line two with more words",
				"line three"
			});
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(3, outputLines.Length);
		}
		
		[TestMethod]
		public void HandleSingleFile_WithSpecialCharacters_ShouldHandleCorrectly()
		{
			var inputFile = GetTempFilePath("line_test_special_input");
			var outputFile = GetTempFilePath("line_test_special_output");
			File.WriteAllLines(inputFile, new[] { "word123 test456 number789" });
			
			var handler = new TextHandler(minAmountOfSymbols: 6, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			var words = outputLines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.IsTrue(words.All(w => w.Length >= 6));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithAllShortWords_ShouldReturnEmptyLine()
		{
			var inputFile = GetTempFilePath("line_test_all_short_input");
			var outputFile = GetTempFilePath("line_test_all_short_output");
			File.WriteAllLines(inputFile, new[] { "a ab" });
			
			var handler = new TextHandler(minAmountOfSymbols: 5, needDeletePunctuationMarks: false);
			
			handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			
			var outputLines = File.ReadAllLines(outputFile);
			Assert.AreEqual(1, outputLines.Length);
			Assert.IsTrue(string.IsNullOrWhiteSpace(outputLines[0]));
		}
		
		[TestMethod]
		public void HandleSingleFile_WithNonexistentFile_ShouldThrowFileNotFoundException()
		{
			var inputFile = Path.Combine(Path.GetTempPath(), "nonexistent_file.txt");
			var outputFile = GetTempFilePath("line_test_nonexistent_output");
			
			var handler = new TextHandler(minAmountOfSymbols: 3, needDeletePunctuationMarks: false);
			
			Assert.Throws<FileNotFoundException>(() =>
			{
				handler.HandleSingleFile(inputFile, outputFile, CancellationToken.None);
			});
		}
	}

}
