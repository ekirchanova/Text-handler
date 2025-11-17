using System;
using System.IO;

namespace textHandlerTest
{
    public abstract class TestBase
    {
        protected string GetTempFilePath(string prefix = "test")
        {
            return Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid()}.txt");
        }

        protected static void SafeDeleteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            try
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
            }
            catch { }
        }

        protected void CleanupFiles(params string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                SafeDeleteFile(filePath);
            }
        }
    }
}

