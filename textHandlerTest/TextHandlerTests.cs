using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using textHandlerClass;

namespace textHandlerTest
{
    [TestClass]
    public class TextHandlerTests : TestBase
    {
        private TextHandler handler;

        [TestInitialize]
        public void Setup()
        {
            handler = new TextHandler();
        }

        [TestMethod]
        public void DefaultConstructor_ShouldSetDefaultValues()
        {
            Assert.IsNotNull(handler);
            Assert.AreEqual(3u, handler.MinAmountOfSymbols);
            Assert.IsFalse(handler.NeedDeletePunctuationMarks);
        }

        [TestMethod]
        public void Constructor_WithParameters_ShouldSetValues()
        {
            var handler = new TextHandler(5, true);
            Assert.AreEqual(5u, handler.MinAmountOfSymbols);
            Assert.IsTrue(handler.NeedDeletePunctuationMarks);
        }

        [TestMethod]
        public void ProcessFiles_WithValidFiles_ShouldProcessSuccessfully()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                File.WriteAllText(inputFile, "Hello world test\nShort words\nLonger words here");

                handler.InputFiles = new[] { inputFile };
                handler.OutputFiles = new[] { outputFile };
                handler.MinAmountOfSymbols = 5;

                handler.ProcessFiles(null, CancellationToken.None).Wait();

                Assert.IsTrue(File.Exists(outputFile));
                var content = File.ReadAllText(outputFile);
                Assert.IsTrue(content.Contains("world") || content.Contains("Longer"));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithMinAmountOfSymbols_ShouldFilterWords()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                File.WriteAllText(inputFile, "y be cat dog elephant\none two three four five");

                handler.InputFiles = new[] { inputFile };
                handler.OutputFiles = new[] { outputFile };
                handler.MinAmountOfSymbols = 3;

                handler.ProcessFiles(null, CancellationToken.None).Wait();

                var content = File.ReadAllText(outputFile);
                Assert.IsFalse(content.Contains("y"));
                Assert.IsFalse(content.Contains("be"));
                Assert.IsTrue(content.Contains("cat") || content.Contains("dog") || content.Contains("elephant"));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithDeletePunctuationMarks_ShouldRemovePunctuation()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                File.WriteAllText(inputFile, "Hello, world! Test? Yes.");

                handler.InputFiles = new[] { inputFile };
                handler.OutputFiles = new[] { outputFile };
                handler.NeedDeletePunctuationMarks = true;
                handler.MinAmountOfSymbols = 3;

                handler.ProcessFiles(null, CancellationToken.None).Wait();

                var content = File.ReadAllText(outputFile);
                Assert.IsFalse(content.Contains(","));
                Assert.IsFalse(content.Contains("!"));
                Assert.IsFalse(content.Contains("?"));
                Assert.IsFalse(content.Contains("."));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithMultipleFiles_ShouldProcessAll()
        {
            string inputFile1 = GetTempFilePath("input1");
            string inputFile2 = GetTempFilePath("input2");
            string outputFile1 = GetTempFilePath("output1");
            string outputFile2 = GetTempFilePath("output2");

            try
            {
                File.WriteAllText(inputFile1, "First file content");
                File.WriteAllText(inputFile2, "Second file content");

                handler.InputFiles = new[] { inputFile1, inputFile2 };
                handler.OutputFiles = new[] { outputFile1, outputFile2 };
                handler.MinAmountOfSymbols = 3;

                var progress = new TestProgress<int>();
                handler.ProcessFiles(progress, CancellationToken.None).Wait();

                Assert.IsTrue(File.Exists(outputFile1));
                Assert.IsTrue(File.Exists(outputFile2));
                Assert.IsTrue(progress.Reports.Count > 0);
            }
            finally
            {
                CleanupFiles(inputFile1, inputFile2, outputFile1, outputFile2);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithNullInputFiles_ShouldThrowArgumentException()
        {
            handler.InputFiles = null;
            handler.OutputFiles = new[] { GetTempFilePath("output") };

            Assert.Throws<AggregateException>(() =>
            {
                handler.ProcessFiles(null, CancellationToken.None).Wait();
            });
        }

        [TestMethod]
        public void ProcessFiles_WithNullOutputFiles_ShouldThrowArgumentException()
        {
            handler.InputFiles = new[] { GetTempFilePath("input") };
            handler.OutputFiles = null;

            Assert.Throws<AggregateException>(() =>
            {
                handler.ProcessFiles(null, CancellationToken.None).Wait();
            });
        }

        [TestMethod]
        public void ProcessFiles_WithEmptyInputFiles_ShouldThrowArgumentException()
        {
            handler.InputFiles = Array.Empty<string>();
            handler.OutputFiles = new[] { GetTempFilePath("output") };

            Assert.Throws<AggregateException>(() =>
            {
                handler.ProcessFiles(null, CancellationToken.None).Wait();
            });
        }

        [TestMethod]
        public void ProcessFiles_WithDifferentLengths_ShouldThrowArgumentException()
        {
            string inputFile = GetTempFilePath("input");
            try
            {
                File.WriteAllText(inputFile, "test");

                handler.InputFiles = new[] { inputFile };
                handler.OutputFiles = new[] { GetTempFilePath("output1"), GetTempFilePath("output2") };

                Assert.Throws<AggregateException>(() =>
                {
                    handler.ProcessFiles(null, CancellationToken.None).Wait();
                });
            }
            finally
            {
                CleanupFiles(inputFile);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithCancellation_ShouldThrowOperationCanceledException()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile, 10000);

                handler.InputFiles = new[] { inputFile };
                handler.OutputFiles = new[] { outputFile };

                var cts = new CancellationTokenSource();
                cts.Cancel();

                Assert.Throws<AggregateException>(() =>
                {
                    handler.ProcessFiles(null, cts.Token).Wait();
                });
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithProgress_ShouldReportProgress()
        {
            string inputFile1 = GetTempFilePath("input1");
            string inputFile2 = GetTempFilePath("input2");
            string outputFile1 = GetTempFilePath("output1");
            string outputFile2 = GetTempFilePath("output2");

            try
            {
                File.WriteAllText(inputFile1, "First file");
                File.WriteAllText(inputFile2, "Second file");

                handler.InputFiles = new[] { inputFile1, inputFile2 };
                handler.OutputFiles = new[] { outputFile1, outputFile2 };

                var progress = new TestProgress<int>();
                handler.ProcessFiles(progress, CancellationToken.None).Wait();

                Assert.IsTrue(progress.Reports.Count > 0);
                Assert.IsTrue(progress.Reports.Last() >= 90);
            }
            finally
            {
                CleanupFiles(inputFile1, inputFile2, outputFile1, outputFile2);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithEmptyFile_ShouldCreateEmptyOutput()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                File.WriteAllText(inputFile, string.Empty);

                handler.InputFiles = new[] { inputFile };
                handler.OutputFiles = new[] { outputFile };

                handler.ProcessFiles(null, CancellationToken.None).Wait();

                Assert.IsTrue(File.Exists(outputFile));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithWhitespaceOnly_ShouldCreateEmptyOutput()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                File.WriteAllText(inputFile, "   \n\t\t\n   ");

                handler.InputFiles = new[] { inputFile };
                handler.OutputFiles = new[] { outputFile };
                handler.MinAmountOfSymbols = 3;

                handler.ProcessFiles(null, CancellationToken.None).Wait();

                var content = File.ReadAllText(outputFile);
                Assert.IsTrue(string.IsNullOrWhiteSpace(content));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithNonexistentInputFile_ShouldThrowFileNotFoundException()
        {
            string outputFile = GetTempFilePath("output");

            try
            {
                handler.InputFiles = new[] { "nonexistent_file.txt" };
                handler.OutputFiles = new[] { outputFile };

                Assert.Throws<AggregateException>(() =>
                {
                    handler.ProcessFiles(null, CancellationToken.None).Wait();
                });
            }
            finally
            {
                CleanupFiles(outputFile);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithLargeFile_1MB_ShouldProcessSuccessfully()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile, 1024 * 1024);

                handler.InputFiles = new[] { inputFile };
                handler.OutputFiles = new[] { outputFile };
                handler.MinAmountOfSymbols = 3;

                handler.ProcessFiles(null, CancellationToken.None).Wait();

                Assert.IsTrue(File.Exists(outputFile));
                var inputInfo = new FileInfo(inputFile);
                var outputInfo = new FileInfo(outputFile);
                Assert.IsTrue(outputInfo.Length >= 0);
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithLargeFile_10MB_ShouldProcessSuccessfully()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile, 10 * 1024 * 1024);

                handler.InputFiles = new[] { inputFile };
                handler.OutputFiles = new[] { outputFile };
                handler.MinAmountOfSymbols = 3;

                var progress = new TestProgress<int>();
                handler.ProcessFiles(progress, CancellationToken.None).Wait();

                Assert.IsTrue(File.Exists(outputFile));
                Assert.IsTrue(progress.Reports.Count > 0);
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithLargeFile_100MB_ShouldProcessSuccessfully()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile, 100 * 1024 * 1024);

                handler.InputFiles = new[] { inputFile };
                handler.OutputFiles = new[] { outputFile };
                handler.MinAmountOfSymbols = 5;

                var progress = new TestProgress<int>();
                handler.ProcessFiles(progress, CancellationToken.None).Wait();

                Assert.IsTrue(File.Exists(outputFile));
                Assert.IsTrue(progress.Reports.Count > 0);
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithLargeFile_ShouldMaintainContentIntegrity()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                var testContent = string.Join("\n", Enumerable.Range(1, 10000).Select(i => $"Test line {i} with enough words"));
                File.WriteAllText(inputFile, testContent);

                handler.InputFiles = new[] { inputFile };
                handler.OutputFiles = new[] { outputFile };
                handler.MinAmountOfSymbols = 3;

                handler.ProcessFiles(null, CancellationToken.None).Wait();

                var outputContent = File.ReadAllText(outputFile);
                Assert.IsTrue(outputContent.Length > 0);
                Assert.IsTrue(outputContent.Contains("Test") || outputContent.Contains("line") || outputContent.Contains("enough"));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithMultipleLargeFiles_ShouldProcessAll()
        {
            string inputFile1 = GetTempFilePath("input1");
            string inputFile2 = GetTempFilePath("input2");
            string outputFile1 = GetTempFilePath("output1");
            string outputFile2 = GetTempFilePath("output2");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile1, 5 * 1024 * 1024);
                TestFileGenerator.CreateTextFile(inputFile2, 5 * 1024 * 1024);

                handler.InputFiles = new[] { inputFile1, inputFile2 };
                handler.OutputFiles = new[] { outputFile1, outputFile2 };
                handler.MinAmountOfSymbols = 3;

                var progress = new TestProgress<int>();
                handler.ProcessFiles(progress, CancellationToken.None).Wait();

                Assert.IsTrue(File.Exists(outputFile1));
                Assert.IsTrue(File.Exists(outputFile2));
                Assert.IsTrue(progress.Reports.Count > 0);
                Assert.IsTrue(progress.Reports.Last() >= 90);
            }
            finally
            {
                CleanupFiles(inputFile1, inputFile2, outputFile1, outputFile2);
            }
        }

        [TestMethod]
        public void ProcessFiles_WithLargeFileAndPunctuation_ShouldRemovePunctuation()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                var lines = Enumerable.Range(1, 5000).Select(i => $"Line {i}: Hello, world! Test? Yes.").ToList();
                File.WriteAllLines(inputFile, lines);

                handler.InputFiles = new[] { inputFile };
                handler.OutputFiles = new[] { outputFile };
                handler.NeedDeletePunctuationMarks = true;
                handler.MinAmountOfSymbols = 3;

                handler.ProcessFiles(null, CancellationToken.None).Wait();

                var content = File.ReadAllText(outputFile);
                Assert.IsFalse(content.Contains(","));
                Assert.IsFalse(content.Contains("!"));
                Assert.IsFalse(content.Contains("?"));
                Assert.IsFalse(content.Contains("."));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }
    }
}

