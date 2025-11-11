using System;
using textHandlerClass;

namespace textHandlerTest
{
	[TestClass]
	public sealed class TextHandlerPropertyTests
	{
		[TestMethod]
		public void DefaultConstructor_ShouldSetDefaultValues()
		{
			var handler = new TextHandler();
			
			Assert.AreEqual(3u, handler.MinAmountOfSymbols);
			Assert.IsTrue(handler.NeedDeletePunctuationMarks);
			Assert.IsNull(handler.InputFiles);
			Assert.IsNull(handler.OutputFiles);
		}
		
		[TestMethod]
		public void ConstructorWithParameters_ShouldSetValues()
		{
			uint minSymbols = 5;
			bool deletePunctuation = false;
			
			var handler = new TextHandler(minSymbols, deletePunctuation);
			
			Assert.AreEqual(minSymbols, handler.MinAmountOfSymbols);
			Assert.AreEqual(deletePunctuation, handler.NeedDeletePunctuationMarks);
		}
		
		[TestMethod]
		public void ConstructorWithMinSymbolsOnly_ShouldSetDefaultPunctuationSetting()
		{
			uint minSymbols = 7;
			
			var handler = new TextHandler(minSymbols);
			
			Assert.AreEqual(minSymbols, handler.MinAmountOfSymbols);
			Assert.IsFalse(handler.NeedDeletePunctuationMarks); 
		}
		
		[TestMethod]
		public void SetMinAmountOfSymbols_ShouldUpdateValue()
		{
			var handler = new TextHandler();
			uint newValue = 10;
			
			handler.MinAmountOfSymbols = newValue;
			
			Assert.AreEqual(newValue, handler.MinAmountOfSymbols);
		}
		
		[TestMethod]
		public void SetMinAmountOfSymbols_Zero_ShouldBeAllowed()
		{
			var handler = new TextHandler();
			
			handler.MinAmountOfSymbols = 0;
			
			Assert.AreEqual(0u, handler.MinAmountOfSymbols);
		}
		
		[TestMethod]
		public void SetMinAmountOfSymbols_LargeValue_ShouldBeAllowed()
		{
			var handler = new TextHandler();
			uint largeValue = uint.MaxValue;
			
			handler.MinAmountOfSymbols = largeValue;
			
			Assert.AreEqual(largeValue, handler.MinAmountOfSymbols);
		}
		
		[TestMethod]
		public void SetNeedDeletePunctuationMarks_ShouldUpdateValue()
		{
			var handler = new TextHandler();
			
			handler.NeedDeletePunctuationMarks = false;
			
			Assert.IsFalse(handler.NeedDeletePunctuationMarks);
		}
		
		[TestMethod]
		public void SetInputFiles_ShouldUpdateValue()
		{
			var handler = new TextHandler();
			string[] files = { "file1.txt", "file2.txt" };
			
			handler.InputFiles = files;
			
			Assert.AreEqual(files, handler.InputFiles);
			Assert.AreEqual(2, handler.InputFiles.Length);
		}
		
		[TestMethod]
		public void SetOutputFiles_ShouldUpdateValue()
		{
			var handler = new TextHandler();
			string[] files = { "output1.txt", "output2.txt" };
			
			handler.OutputFiles = files;
			
			Assert.AreEqual(files, handler.OutputFiles);
			Assert.AreEqual(2, handler.OutputFiles.Length);
		}
	}

}
