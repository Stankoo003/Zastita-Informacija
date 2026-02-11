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

            // Kreiraj encrypted folder ako ne postoji
            Directory.CreateDirectory("encrypted");

            watcher = new FileSystemWatcher(targetPath);

            // Prati sve fajlove
            watcher.Filter = "*.*";

            // Prati kreiranje novih fajlova
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;

            // Event kada se doda novi fajl
            watcher.Created += OnFileCreated;

            // Pokreni praćenje
            watcher.EnableRaisingEvents = true;

            onMessage?.Invoke($"👁️ FSW pokrenut - pratim folder: {targetPath}");
            onMessage?.Invoke($"🔐 Algoritam: {algorithm}");
            Logger.Log($"FSW started watching: {targetPath}");
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Sačekaj da se fajl potpuno zapiše
                System.Threading.Thread.Sleep(500);

                onMessage?.Invoke($"📁 Detektovan novi fajl: {Path.GetFileName(e.FullPath)}");
                Logger.Log($"FSW detected file: {e.Name}");

                // Pročitaj fajl
                byte[] fileData = File.ReadAllBytes(e.FullPath);
                onMessage?.Invoke($"📖 Veličina: {fileData.Length} bajtova");

                // Enkriptuj
                onMessage?.Invoke($"🔒 Automatski enkriptujem...");
                byte[] encryptedData = CryptoHelper.EncryptData(fileData, algorithm);

                // Računaj heš
                string fileHash = TigerHash.ComputeHash(encryptedData);

                // Kreiraj metadata
                string metadata = MetadataHandler.CreateMetadata(
                    Path.GetFileName(e.FullPath),
                    encryptedData,
                    algorithm,
                    "Tiger (SHA1)",
                    fileHash
                );

                // Sačuvaj u encrypted folder
                string encryptedPath = Path.Combine("encrypted", Path.GetFileName(e.FullPath) + ".enc");
                string metadataPath = encryptedPath + ".meta";

                File.WriteAllBytes(encryptedPath, encryptedData);
                File.WriteAllText(metadataPath, metadata);

                onMessage?.Invoke($"✅ Fajl enkriptovan: {encryptedPath}");
                onMessage?.Invoke($"🔑 Heš: {fileHash.Substring(0, 16)}...");
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
