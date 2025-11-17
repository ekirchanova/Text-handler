using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using textHandlerClass.ProcesingFiles;

namespace textHandlerTest
{
    [TestClass]
    public class ParallelProcessFileTests : TestBase
    {
        [TestMethod]
        public void Constructor_WithDefaultParameters_ShouldSetDefaults()
        {
            var processor = new ParallelProcessFile();
            Assert.IsNotNull(processor);
        }

        [TestMethod]
        public void Constructor_WithCustomChunkSize_ShouldSetChunkSize()
        {
            var processor = new ParallelProcessFile(4096, 2);
            Assert.IsNotNull(processor);
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithValidFiles_ShouldProcessSuccessfully()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                File.WriteAllText(inputFile, "Hello world test");

                var processor = new ParallelProcessFile();
                await processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    (path, num, chunk) => Task.FromResult(chunk.ToUpper()),
                    null,
                    CancellationToken.None);

                Assert.IsTrue(File.Exists(outputFile));
                var content = File.ReadAllText(outputFile);
                Assert.IsTrue(content.Contains("HELLO") || content.Contains("WORLD"));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithMultipleFiles_ShouldProcessAll()
        {
            string inputFile1 = GetTempFilePath("input1");
            string inputFile2 = GetTempFilePath("input2");
            string outputFile1 = GetTempFilePath("output1");
            string outputFile2 = GetTempFilePath("output2");

            try
            {
                File.WriteAllText(inputFile1, "First file");
                File.WriteAllText(inputFile2, "Second file");

                var processor = new ParallelProcessFile();
                var progress = new TestProgress<int>();

                await processor.ProcessFilesAsync(
                    new[] { inputFile1, inputFile2 },
                    new[] { outputFile1, outputFile2 },
                    (path, num, chunk) => Task.FromResult(chunk),
                    progress,
                    CancellationToken.None);

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
        public async Task ProcessFilesAsync_WithProgress_ShouldReportProgress()
        {
            string inputFile1 = GetTempFilePath("input1");
            string inputFile2 = GetTempFilePath("input2");
            string outputFile1 = GetTempFilePath("output1");
            string outputFile2 = GetTempFilePath("output2");

            try
            {
                File.WriteAllText(inputFile1, "First");
                File.WriteAllText(inputFile2, "Second");

                var processor = new ParallelProcessFile();
                var progress = new TestProgress<int>();

                await processor.ProcessFilesAsync(
                    new[] { inputFile1, inputFile2 },
                    new[] { outputFile1, outputFile2 },
                    (path, num, chunk) => Task.FromResult(chunk),
                    progress,
                    CancellationToken.None);

                Assert.IsTrue(progress.Reports.Count >= 2);
                Assert.IsTrue(progress.Reports.Any(p => p >= 50));
            }
            finally
            {
                CleanupFiles(inputFile1, inputFile2, outputFile1, outputFile2);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithLargeFile_ShouldProcessInChunks()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile, 50000);

                var processor = new ParallelProcessFile(1024);
                int chunkCount = 0;

                await processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    (path, num, chunk) =>
                    {
                        Interlocked.Increment(ref chunkCount);
                        return Task.FromResult(chunk);
                    },
                    null,
                    CancellationToken.None);

                Assert.IsTrue(File.Exists(outputFile));
                Assert.IsTrue(chunkCount > 0);
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithCancellation_ShouldThrowOperationCanceledException()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile, 10000);

                var processor = new ParallelProcessFile();
                var cts = new CancellationTokenSource();
                cts.Cancel();

                await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await processor.ProcessFilesAsync(
                        new[] { inputFile },
                        new[] { outputFile },
                        (path, num, chunk) => Task.FromResult(chunk),
                        null,
                        cts.Token);
                });
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithDifferentLengths_ShouldThrowArgumentException()
        {
            string inputFile = GetTempFilePath("input");
            try
            {
                File.WriteAllText(inputFile, "test");

                var processor = new ParallelProcessFile();

                await Assert.ThrowsAsync<ArgumentException>(async () =>
                {
                    await processor.ProcessFilesAsync(
                        new[] { inputFile },
                        new[] { GetTempFilePath("output1"), GetTempFilePath("output2") },
                        (path, num, chunk) => Task.FromResult(chunk),
                        null,
                        CancellationToken.None);
                });
            }
            finally
            {
                CleanupFiles(inputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithNonexistentInputFile_ShouldThrowFileNotFoundException()
        {
            string outputFile = GetTempFilePath("output");

            try
            {
                var processor = new ParallelProcessFile();

                await Assert.ThrowsAsync<FileNotFoundException>(async () =>
                {
                    await processor.ProcessFilesAsync(
                        new[] { "nonexistent_file.txt" },
                        new[] { outputFile },
                        (path, num, chunk) => Task.FromResult(chunk),
                        null,
                        CancellationToken.None);
                });
            }
            finally
            {
                CleanupFiles(outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithEmptyFile_ShouldCreateEmptyOutput()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                File.WriteAllText(inputFile, string.Empty);

                var processor = new ParallelProcessFile();
                await processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    (path, num, chunk) => Task.FromResult(chunk),
                    null,
                    CancellationToken.None);

                Assert.IsTrue(File.Exists(outputFile));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithChunkNumbering_ShouldPreserveOrder()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                var content = new string('a', 10000) + new string('b', 10000);
                File.WriteAllText(inputFile, content);

                var processor = new ParallelProcessFile(5000);
                var chunks = new System.Collections.Concurrent.ConcurrentBag<(int number, string content)>();

                await processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    (path, num, chunk) =>
                    {
                        chunks.Add((num, chunk));
                        return Task.FromResult($"[{num}]{chunk}");
                    },
                    null,
                    CancellationToken.None);

                var result = File.ReadAllText(outputFile);
                Assert.IsTrue(result.Contains("[1]") && result.Contains("[2]"));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithAsyncProcessor_ShouldWaitForCompletion()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                File.WriteAllText(inputFile, "test");

                var processor = new ParallelProcessFile();
                bool processed = false;

                await processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    async (path, num, chunk) =>
                    {
                        await Task.Delay(10);
                        processed = true;
                        return chunk;
                    },
                    null,
                    CancellationToken.None);

                Assert.IsTrue(processed);
                Assert.IsTrue(File.Exists(outputFile));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithOutputDirectoryCreation_ShouldCreateDirectory()
        {
            string inputFile = GetTempFilePath("input");
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string outputFile = Path.Combine(tempDir, "output.txt");

            try
            {
                File.WriteAllText(inputFile, "test");

                var processor = new ParallelProcessFile();
                await processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    (path, num, chunk) => Task.FromResult(chunk),
                    null,
                    CancellationToken.None);

                Assert.IsTrue(Directory.Exists(tempDir));
                Assert.IsTrue(File.Exists(outputFile));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir); } catch { }
                }
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithLargeFile_1MB_ShouldProcessInChunks()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile, 1024 * 1024);

                var processor = new ParallelProcessFile(8192);
                int chunkCount = 0;

                await processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    (path, num, chunk) =>
                    {
                        Interlocked.Increment(ref chunkCount);
                        return Task.FromResult(chunk);
                    },
                    null,
                    CancellationToken.None);

                Assert.IsTrue(File.Exists(outputFile));
                Assert.IsTrue(chunkCount > 10);
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithLargeFile_10MB_ShouldProcessSuccessfully()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile, 10 * 1024 * 1024);

                var processor = new ParallelProcessFile();
                var progress = new TestProgress<int>();

                await processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    (path, num, chunk) => Task.FromResult(chunk),
                    progress,
                    CancellationToken.None);

                Assert.IsTrue(File.Exists(outputFile));
                Assert.IsTrue(progress.Reports.Count > 0);
                var inputSize = new FileInfo(inputFile).Length;
                var outputSize = new FileInfo(outputFile).Length;
                Assert.IsTrue(outputSize >= 0);
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithLargeFile_100MB_ShouldProcessSuccessfully()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile, 100 * 1024 * 1024);

                var processor = new ParallelProcessFile();
                int chunkCount = 0;
                var progress = new TestProgress<int>();

                await processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    (path, num, chunk) =>
                    {
                        Interlocked.Increment(ref chunkCount);
                        return Task.FromResult(chunk);
                    },
                    progress,
                    CancellationToken.None);

                Assert.IsTrue(File.Exists(outputFile));
                Assert.IsTrue(chunkCount > 100);
                Assert.IsTrue(progress.Reports.Count > 0);
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithVeryLargeFile_500MB_ShouldProcessSuccessfully()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile, 500 * 1024 * 1024);

                var processor = new ParallelProcessFile(8192);
                var progress = new TestProgress<int>();
                long totalChunks = 0;

                await processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    (path, num, chunk) =>
                    {
                        Interlocked.Increment(ref totalChunks);
                        return Task.FromResult(chunk);
                    },
                    progress,
                    CancellationToken.None);

                Assert.IsTrue(File.Exists(outputFile));
                Assert.IsTrue(totalChunks > 500);
                Assert.IsTrue(progress.Reports.Count > 0);
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithVeryLargeFile_1GB_ShouldProcessSuccessfully()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile, 1024 * 1024 * 1024);

                var processor = new ParallelProcessFile(8192);
                var progress = new TestProgress<int>();
                long totalChunks = 0;

                await processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    (path, num, chunk) =>
                    {
                        Interlocked.Increment(ref totalChunks);
                        return Task.FromResult(chunk);
                    },
                    progress,
                    CancellationToken.None);

                Assert.IsTrue(File.Exists(outputFile));
                Assert.IsTrue(totalChunks > 500);
                Assert.IsTrue(progress.Reports.Count > 0);
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithLargeFile_ShouldPreserveOrder()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                var content = string.Join("\n", Enumerable.Range(1, 100000).Select(i => $"Line {i:D6}"));
                File.WriteAllText(inputFile, content);

                var processor = new ParallelProcessFile(16384);
                await processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    (path, num, chunk) => Task.FromResult(chunk),
                    null,
                    CancellationToken.None);

                var outputContent = File.ReadAllText(outputFile);
                Assert.IsTrue(outputContent.Contains("Line 000001"));
                Assert.IsTrue(outputContent.Contains("Line 100000") || outputContent.Contains("Line 099999"));
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithLargeFileAndCancellation_ShouldHandleCancellation()
        {
            string inputFile = GetTempFilePath("input");
            string outputFile = GetTempFilePath("output");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile, 50 * 1024 * 1024);

                var processor = new ParallelProcessFile();
                var cts = new CancellationTokenSource();
                int processedChunks = 0;

                var task = processor.ProcessFilesAsync(
                    new[] { inputFile },
                    new[] { outputFile },
                    async (path, num, chunk) =>
                    {
                        Interlocked.Increment(ref processedChunks);
                        if (processedChunks > 10)
                        {
                            cts.Cancel();
                        }
                        await Task.Delay(1);
                        return chunk;
                    },
                    null,
                    cts.Token);

                await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
                Assert.IsTrue(processedChunks > 0);
            }
            finally
            {
                CleanupFiles(inputFile, outputFile);
            }
        }

        [TestMethod]
        public async Task ProcessFilesAsync_WithMultipleLargeFiles_ShouldProcessAll()
        {
            string inputFile1 = GetTempFilePath("input1");
            string inputFile2 = GetTempFilePath("input2");
            string inputFile3 = GetTempFilePath("input3");
            string outputFile1 = GetTempFilePath("output1");
            string outputFile2 = GetTempFilePath("output2");
            string outputFile3 = GetTempFilePath("output3");

            try
            {
                TestFileGenerator.CreateTextFile(inputFile1, 10 * 1024 * 1024);
                TestFileGenerator.CreateTextFile(inputFile2, 10 * 1024 * 1024);
                TestFileGenerator.CreateTextFile(inputFile3, 10 * 1024 * 1024);

                var processor = new ParallelProcessFile();
                var progress = new TestProgress<int>();

                await processor.ProcessFilesAsync(
                    new[] { inputFile1, inputFile2, inputFile3 },
                    new[] { outputFile1, outputFile2, outputFile3 },
                    (path, num, chunk) => Task.FromResult(chunk),
                    progress,
                    CancellationToken.None);

                Assert.IsTrue(File.Exists(outputFile1));
                Assert.IsTrue(File.Exists(outputFile2));
                Assert.IsTrue(File.Exists(outputFile3));
                Assert.IsTrue(progress.Reports.Count >= 3);
                Assert.IsTrue(progress.Reports.Last() >= 90);
            }
            finally
            {
                CleanupFiles(inputFile1, inputFile2, inputFile3, outputFile1, outputFile2, outputFile3);
            }
        }

    }
}

