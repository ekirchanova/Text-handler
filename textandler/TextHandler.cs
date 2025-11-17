using System.Collections.Concurrent;
using System.IO;
using textHandlerClass.ProcesingFiles;

namespace textHandlerClass
{
    public partial class TextHandler
    {
        private static char[] delimiters;
        static TextHandler()
        {
            delimiters = new char[]{ ' ', '\t', '\n', '\r' };
        }
        public uint MinAmountOfSymbols { get; set; } = 3;
        public bool NeedDeletePunctuationMarks { get; set; } = false;
        public string[] InputFiles { get; set; }
        public string[] OutputFiles { get; set; }
        public TextHandler() {}
        public TextHandler(uint minAmountOfSymbols, bool needDeletePunctuationMarks = false)
        {
            MinAmountOfSymbols = minAmountOfSymbols;
            NeedDeletePunctuationMarks = needDeletePunctuationMarks;
        }
        private static string DeletePunctuationMarks(string line)
        {
            return new string(line.Where(c => !char.IsPunctuation(c)).ToArray());
        }
        private string ProcessLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return string.Empty;

            if (NeedDeletePunctuationMarks)
                line = DeletePunctuationMarks(line);

            string[] words = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<string> filteredWords = words.Where(word => word.Length >= MinAmountOfSymbols);
            return string.Join(" ", filteredWords);
        }

        private bool CheckFileProcessPosibility(string[] inputFiles, string[] outputFiles)
        {
            if (inputFiles == null || outputFiles == null)
                return false;

            return inputFiles.Length > 0 && outputFiles.Length > 0 && outputFiles.Length == inputFiles.Length;
        }

        public async Task ProcessFiles(IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (!CheckFileProcessPosibility(InputFiles, OutputFiles))
                throw new ArgumentException("Don't correct input or(and) output files.");

            cancellationToken.ThrowIfCancellationRequested();
            var processor = new ParallelProcessFile();
            await processor.ProcessFilesAsync(InputFiles, OutputFiles,
                (inputPath, chunkNumber, chunk) => Task.FromResult(ProcessLine(chunk)), progress,
                cancellationToken);
        }
        
    }
}
