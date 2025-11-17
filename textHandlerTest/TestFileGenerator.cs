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
					if (i > 0 && random.Next(100) < 15) 
					{
						lineBuilder.Append(' ');
					}
					else if (i > 0 && random.Next(100) < 5) 
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
	}

}
