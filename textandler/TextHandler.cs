using System.Collections.Concurrent;
using System.IO;

namespace textHandlerClass
{
    public partial class TextHandler
    {
        private static char[] delimiters;
        static TextHandler()
        {
            delimiters = new char[]{ ' ', '\t' };
        }
        public uint MinAmountOfSymbols { get; set; } = 3;
        public bool NeedDeletePunctuationMarks { get; set; } = true;

        public string[] InputFiles { get; set; }
        public string[] OutputFiles { get; set; }
        public TextHandler() {}
        public TextHandler(uint minAmountOfSymbols, bool needDeletePunctuationMarks = false)
        {
            MinAmountOfSymbols = minAmountOfSymbols;
            NeedDeletePunctuationMarks = needDeletePunctuationMarks;
        }
        private string DeletePunctuationMarks(string line)
        {
            return new string(line.Where(c => !char.IsPunctuation(c)).ToArray());
        }
        private string ProcessLine(string line)
        {
            if (line.IsWhiteSpace()) return string.Empty;

            if (NeedDeletePunctuationMarks)
                line = DeletePunctuationMarks(line);

            string[] words = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<string> filteredWords = words.Where(word => word.Length >= MinAmountOfSymbols);
            return string.Join(" ", filteredWords);
        }

        public bool CheckFileProcessPosibility()
        {
            return InputFiles.Length > 0 && OutputFiles.Length > 0;
        }
        public void HandleSingleFile(string InputFile,  string OutputFile, CancellationToken cancellationToken)
        {
            if (!File.Exists(InputFile))
                throw new FileNotFoundException($"Input file not found: {InputFiles}");
            var lines = File.ReadAllLines(InputFile);
            var results = new ConcurrentBag<string>();

            Parallel.ForEach(lines, new ParallelOptions { CancellationToken = cancellationToken }, line =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string processedLine = ProcessLine(line);
                    results.Add(processedLine);
                }
                catch (OperationCanceledException)
                {
                    throw; 
                }
                catch (Exception e)
                {
                    results.Add(e.Message);
                }
            });
            string outputDir = Path.GetDirectoryName(OutputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            File.WriteAllLines(OutputFile, results);
        }

        public void HandleFilesMas(string[] InputFiles, string[] OutputFiles, IProgress<int> progress, CancellationToken cancellationToken)
        {
            int size = InputFiles.Length;
            if (OutputFiles.Length != size)
                throw new ArgumentException("Output files count must match input files count.");

            int completed = 0;
            Parallel.For(0, size, new ParallelOptions { CancellationToken = cancellationToken }, i =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                HandleSingleFile(InputFiles[i], OutputFiles[i], cancellationToken);
                int done = Interlocked.Increment(ref completed);
                int percent = (int)Math.Round((double)done * 100 / size);
                progress?.Report(percent);
            });
        }

        public void ProcessFiles(IProgress<int> progress, CancellationToken cancellationToken)
        {
            HandleFilesMas(InputFiles, OutputFiles, progress, cancellationToken);
        }
        
    }
}
