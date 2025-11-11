using System;
using System.IO;
using System.Linq;
using System.Text;

namespace textHandlerTest
{
	public static class TestFileGenerator
	{
		public static long CreateTextFile(string filePath, long targetSizeBytes, int minLineLength = 10, int maxLineLength = 200, int? randomSeed = null)
		{
			var random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ,.;:!?()[]{}\"\'-";
			const string punctuation = ".,;:!?()[]{}\"'-";
			
			using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
			long bytesWritten = 0;
			
			while (bytesWritten < targetSizeBytes)
			{
				int lineLength = random.Next(minLineLength, maxLineLength + 1);
				var lineBuilder = new StringBuilder(lineLength);
				
				for (int i = 0; i < lineLength; i++)
				{
					// Случайно добавляем слова, пробелы и знаки препинания
					if (i > 0 && random.Next(100) < 15) // 15% вероятность пробела
					{
						lineBuilder.Append(' ');
					}
					else if (i > 0 && random.Next(100) < 5) // 5% вероятность знака препинания
					{
						lineBuilder.Append(punctuation[random.Next(punctuation.Length)]);
					}
					else
					{
						lineBuilder.Append(chars[random.Next(chars.Length)]);
					}
				}
				
				string line = lineBuilder.ToString();
				writer.WriteLine(line);
				
				long lineBytes = Encoding.UTF8.GetByteCount(line + Environment.NewLine);
				bytesWritten += lineBytes;
			}
			
			return bytesWritten;
		}

		public static void CreateTextFileByLines(string filePath, int lineCount, int wordsPerLine = 10, int? randomSeed = null)
		{
			var random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			
			using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
			
			for (int line = 0; line < lineCount; line++)
			{
				var lineBuilder = new StringBuilder();
				int actualWords = random.Next(wordsPerLine / 2, wordsPerLine * 2 + 1);
				
				for (int word = 0; word < actualWords; word++)
				{
					if (lineBuilder.Length > 0)
						lineBuilder.Append(' ');
					
					// Генерируем слово длиной от 3 до 15 символов
					int wordLength = random.Next(3, 16);
					for (int i = 0; i < wordLength; i++)
					{
						lineBuilder.Append(chars[random.Next(chars.Length)]);
					}
				}
				
				writer.WriteLine(lineBuilder.ToString());
			}
		}
		public static void CreateSmallTestFile(string filePath)
		{
			var lines = new[]
			{
				"Hello world this is a test",
				"Short words like a and I",
				"Longer words like programming and development",
				"Some punctuation marks, here! And there?",
				"Empty line follows:",
				"",
				"More text with numbers 123 456 789"
			};
			File.WriteAllLines(filePath, lines);
		}
		public static void CreateEmptyLinesFile(string filePath, int lineCount = 100)
		{
			using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
			for (int i = 0; i < lineCount; i++)
			{
				writer.WriteLine(string.Empty);
			}
		}
		
		public static void CreateWhitespaceFile(string filePath, long targetSizeBytes = 1000)
		{
			using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
			long bytesWritten = 0;
			
			while (bytesWritten < targetSizeBytes)
			{
				string line = new string(' ', 100) + "\t\t" + new string(' ', 50);
				writer.WriteLine(line);
				bytesWritten += Encoding.UTF8.GetByteCount(line + Environment.NewLine);
			}
		}
	}

}
