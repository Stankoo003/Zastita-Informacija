using System;
using System.IO;

using Logging;

namespace FileOps
{
    public class FileHandler
    {
        public static byte[] ReadFile(string filepath)
        {
            Logger.Log($"Čitam datoteku: {filepath}");
            return File.ReadAllBytes(filepath);
        }

        public static void WriteFile(string filepath, byte[] data)
        {
            string directory = Path.GetDirectoryName(filepath) ?? "";
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            
            File.WriteAllBytes(filepath, data);
            Logger.Log($"Zapisao datoteku: {filepath}");
        }

        public static string GetEncryptedFilename(string originalFilename)
        {
            return Path.Combine("encrypted", Path.GetFileNameWithoutExtension(originalFilename) + ".enc");
        }
    }
}
