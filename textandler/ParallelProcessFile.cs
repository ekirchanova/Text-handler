using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace textHandlerClass.ProcesingFiles
{

    public class ParallelProcessFile
    {
        private readonly int _chunkSize;
        private readonly int _maxChunkParallelism;
            
        public ParallelProcessFile(
            int chunkSize = 8192,
            int maxChunkParallelism = -1)
        {
            _chunkSize = chunkSize;
            _maxChunkParallelism = maxChunkParallelism == -1 ? Environment.ProcessorCount : maxChunkParallelism;
        }

        private async Task ProcessFilesAsync(
            Dictionary<string, string> fileMappings,
            Func<string, int, string, Task<string>> processChunkAsync,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int size = fileMappings.Count;

            int completed = 0;
            foreach (var mapping in fileMappings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessSingleFileAsync(
                    mapping.Key,
                    mapping.Value,
                    processChunkAsync,
                    cancellationToken);

                int done = Interlocked.Increment(ref completed);
                int percent = (int)Math.Round((double)done * 100 / size);
                progress?.Report(percent);
            }
        }

        public async Task ProcessFilesAsync(
            IEnumerable<string> inputFilePaths,
            IEnumerable<string> outputFilePaths,
            Func<string, int, string, Task<string>> processChunkAsync,
            IProgress<int> progress = null, 
            CancellationToken cancellationToken = default)
        {
            if(inputFilePaths.Count() != outputFilePaths.Count())
                throw new ArgumentException("Output files count must match input files count.");

            cancellationToken.ThrowIfCancellationRequested();
            var fileMappings = inputFilePaths.Zip(outputFilePaths,
                    (input, output) => new { Input = input, Output = output })
                .ToDictionary(x => x.Input, x => x.Output);

            await ProcessFilesAsync(fileMappings, processChunkAsync, progress, cancellationToken);
        }

        private async Task ProcessSingleFileAsync(
            string inputFilePath,
            string outputFilePath,
            Func<string, int, string, Task<string>> processChunkAsync,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outputDirectory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            try
            {
                var tempOutputPath = outputFilePath + ".tmp";
                using var outputStream = new FileStream(
                    tempOutputPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: _chunkSize,
                    useAsync: true);

                await using var outputWriter = new StreamWriter(outputStream, Encoding.UTF8);

                var processedChunks = await ReadAndProcessChunksParallelAsync(
                    inputFilePath, processChunkAsync, cancellationToken);

                await WriteOrderedResultsAsync(processedChunks, outputWriter, cancellationToken);

                await outputWriter.FlushAsync();
                outputWriter.Close();

                if (File.Exists(outputFilePath))
                    File.Delete(outputFilePath);
                File.Move(tempOutputPath, outputFilePath);

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<ConcurrentDictionary<int, string>> ReadAndProcessChunksParallelAsync(
            string inputFilePath,
            Func<string, int, string, Task<string>> processChunkAsync,
            CancellationToken cancellationToken)
        {
            var processedChunks = new ConcurrentDictionary<int, string>();
            using var semaphore = new SemaphoreSlim(_maxChunkParallelism);

            try
            {
                using var inputStream = new FileStream(
                    inputFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: _chunkSize,
                    useAsync: true);

                using var reader = new StreamReader(inputStream, Encoding.UTF8);

                var chunkTasks = new List<Task>();
                int chunkNumber = 0;
                char[] buffer = new char[_chunkSize];

                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    int charsRead = await reader.ReadBlockAsync(buffer, 0, _chunkSize);
                    if (charsRead == 0) break;

                    string chunkContent = new string(buffer, 0, charsRead);
                    ++chunkNumber;

                    await semaphore.WaitAsync(cancellationToken);

                    var chunkTask = Task.Run(async () =>
                    {
                        try
                        {
                            var processedContent = await processChunkAsync(
                                inputFilePath, chunkNumber, chunkContent);

                            processedChunks[chunkNumber] = processedContent;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken);

                    chunkTasks.Add(chunkTask);
                    Array.Clear(buffer, 0, charsRead);
                }

                await Task.WhenAll(chunkTasks);
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            return processedChunks;
        }

        private async Task WriteOrderedResultsAsync(
            ConcurrentDictionary<int, string> processedChunks,
            StreamWriter outputWriter,
            CancellationToken cancellationToken)
        {
            var orderedChunks = processedChunks
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => kvp.Value);

            foreach (var chunk in orderedChunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await outputWriter.WriteAsync(chunk);
            }

            await outputWriter.FlushAsync();
        }
    }
}