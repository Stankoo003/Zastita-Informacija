using System;
using System.IO;
using CryptoHelperNamespace;
using Hashing;
using FileOps;
using Logging;

namespace FileWatching
{
    public class FileSystemWatcherService
    {
        private FileSystemWatcher? watcher;
        private string algorithm;
        private Action<string>? onMessage;

        public FileSystemWatcherService(string algorithm, Action<string>? onMessage = null)
        {
            this.algorithm = algorithm;
            this.onMessage = onMessage;
        }

        public void StartWatching(string targetPath)
        {
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
                onMessage?.Invoke($"📁 Kreiran folder: {targetPath}");
            }

            
            Directory.CreateDirectory("encrypted");

            watcher = new FileSystemWatcher(targetPath);

            
            watcher.Filter = "*.*";

            
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;

            
            watcher.Created += OnFileCreated;

            
            watcher.EnableRaisingEvents = true;

            onMessage?.Invoke($"👁️ FSW pokrenut - pratim folder: {targetPath}");
            onMessage?.Invoke($"🔐 Algoritam: {algorithm}");
            Logger.Log($"FSW started watching: {targetPath}");
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                
                System.Threading.Thread.Sleep(500);

                onMessage?.Invoke($"📁 Detektovan novi fajl: {Path.GetFileName(e.FullPath)}");
                Logger.Log($"FSW detected file: {e.Name}");

                
                byte[] fileData = File.ReadAllBytes(e.FullPath);
                onMessage?.Invoke($"📖 Veličina: {fileData.Length} bajtova");

                
                onMessage?.Invoke($"🔒 Automatski enkriptujem...");
                byte[] encryptedData = CryptoHelper.EncryptData(fileData, algorithm);

                
                var tigerHash = new Hashing.TigerHash();
                string receivedHash = tigerHash.ComputeHash(encryptedData);

                
                string metadata = MetadataHandler.CreateMetadata(
                    Path.GetFileName(e.FullPath),
                    encryptedData,
                    algorithm,
                    "Tiger (SHA1)",
                    receivedHash
                );

                
                string encryptedPath = Path.Combine("encrypted", Path.GetFileName(e.FullPath) + ".enc");
                string metadataPath = encryptedPath + ".meta";

                File.WriteAllBytes(encryptedPath, encryptedData);
                File.WriteAllText(metadataPath, metadata);

                onMessage?.Invoke($"✅ Fajl enkriptovan: {encryptedPath}");
                onMessage?.Invoke($"🔑 Heš: {receivedHash.Substring(0, 16)}...");
                Logger.Log($"FSW encrypted file: {encryptedPath}");
            }
            catch (Exception ex)
            {
                onMessage?.Invoke($"❌ Greška: {ex.Message}");
                Logger.Log($"FSW error: {ex.Message}");
            }
        }

        public void StopWatching()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                onMessage?.Invoke("⏹️ FSW zaustavljen");
                Logger.Log("FSW stopped");
            }
        }
    }
}
