using Microsoft.CodeCoverage.Core.Reports.Coverage;
using System;
using System.Text;
using textHandlerClass;

namespace textHandlerTest
{
    [TestClass]
    public sealed class ConstructorTest
    {
        [TestMethod]
        public void DefaultCreationTextHandler()
        {
           textHandlerClass.TextHandler handler = new textHandlerClass.TextHandler();
           Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void CreationWithParametersTextHandler()
        {
            uint size = 3;
            textHandlerClass.TextHandler handler = new textHandlerClass.TextHandler(size, false);
            Assert.AreEqual(size, handler.MinAmountOfSymbols);
            Assert.IsNotNull(handler);
        }
        [TestMethod]
        public void CreationDefaultAndAddParametersTextHandler()
        {
            textHandlerClass.TextHandler handler = new textHandlerClass.TextHandler();
            uint size = 3;
            handler.MinAmountOfSymbols = size;
            Assert.AreEqual(size, handler.MinAmountOfSymbols);
            Assert.IsNotNull(handler);
        }
        [TestMethod]
        public void CreationAndChangeParametersTextHandler()
        {
            uint initialSize = 1;
            textHandlerClass.TextHandler handler = new textHandlerClass.TextHandler(initialSize);
            Assert.AreEqual(initialSize, handler.MinAmountOfSymbols);
            uint size = 3;
            handler.MinAmountOfSymbols = size;
            Assert.AreEqual(size, handler.MinAmountOfSymbols);
        }
    }
 }
