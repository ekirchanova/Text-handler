using System;
using textHandlerClass;

namespace textHandlerTest
{
	[TestClass]
	public sealed class TextHandlerCheckFileTests
	{
		[TestMethod]
		public void CheckFileProcessPosibility_WithValidArrays_ShouldReturnTrue()
		{
			var handler = new TextHandler();
			handler.InputFiles = new[] { "input1.txt", "input2.txt" };
			handler.OutputFiles = new[] { "output1.txt", "output2.txt" };
			
			bool result = handler.CheckFileProcessPosibility();
			
			Assert.IsTrue(result);
		}
		
		[TestMethod]
		public void CheckFileProcessPosibility_WithEmptyInputFiles_ShouldReturnFalse()
		{
			var handler = new TextHandler();
			handler.InputFiles = Array.Empty<string>();
			handler.OutputFiles = new[] { "output1.txt" };
			
			bool result = handler.CheckFileProcessPosibility();
			
			Assert.IsFalse(result);
		}
		
		[TestMethod]
		public void CheckFileProcessPosibility_WithEmptyOutputFiles_ShouldReturnFalse()
		{
			var handler = new TextHandler();
			handler.InputFiles = new[] { "input1.txt" };
			handler.OutputFiles = Array.Empty<string>();
			
			bool result = handler.CheckFileProcessPosibility();
			
			Assert.IsFalse(result);
		}
		
		[TestMethod]
		public void CheckFileProcessPosibility_WithBothEmptyArrays_ShouldReturnFalse()
		{
			var handler = new TextHandler();
			handler.InputFiles = Array.Empty<string>();
			handler.OutputFiles = Array.Empty<string>();
			
			bool result = handler.CheckFileProcessPosibility();
			
			Assert.IsFalse(result);
		}
		
		[TestMethod]
		public void CheckFileProcessPosibility_WithSingleFile_ShouldReturnTrue()
		{
			var handler = new TextHandler();
			handler.InputFiles = new[] { "input.txt" };
			handler.OutputFiles = new[] { "output.txt" };
			
			bool result = handler.CheckFileProcessPosibility();
			
			Assert.IsTrue(result);
		}
		
		[TestMethod]
		public void CheckFileProcessPosibility_WithNullInputFiles_ShouldThrowNullReferenceException()
		{
			var handler = new TextHandler();
			handler.InputFiles = null;
			handler.OutputFiles = new[] { "output.txt" };
			
			Assert.Throws<NullReferenceException>(() =>
			{
				handler.CheckFileProcessPosibility();
			});
		}
		
		[TestMethod]
		public void CheckFileProcessPosibility_WithNullOutputFiles_ShouldThrowNullReferenceException()
		{
			var handler = new TextHandler();
			handler.InputFiles = new[] { "input.txt" };
			handler.OutputFiles = null;
			
			Assert.Throws<NullReferenceException>(() =>
			{
				handler.CheckFileProcessPosibility();
			});
		}
	}

}
