using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CryptoHelperNamespace;
using FileOps;
using Logging;

namespace Network
{
    public class TCPServer
    {
        public static async Task<string?> StartServerAsync(int port = 5000, Action<string>? onMessage = null)
        {
            // Kreiraj received folder ako ne postoji
            string receivedDir = "received";
            if (!Directory.Exists(receivedDir))
            {
                Directory.CreateDirectory(receivedDir);
                onMessage?.Invoke($"📁 Kreiran folder: {receivedDir}");
            }

            onMessage?.Invoke($"TCP Server pokrenut na portu {port}");
            Console.WriteLine($"🟢 Server sluša na portu {port}...");
            Console.WriteLine("Čekam klijenta za razmenu datoteka...");

            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            listener.Start();

            try
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                onMessage?.Invoke("✅ Klijent se povezao!");
                Console.WriteLine("✅ Klijent se povezao!");

                NetworkStream stream = client.GetStream();

                byte[] buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string metadataJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var metadata = MetadataHandler.ReadMetadata(metadataJson);

                // Pošalji metadata info
                onMessage?.Invoke("📋 === METADATA ===");
                onMessage?.Invoke($"   Fajl: {metadata.Filename}");
                onMessage?.Invoke($"   Veličina: {metadata.FileSize} bajtova");
                onMessage?.Invoke($"   Datum: {metadata.CreatedDate}");
                onMessage?.Invoke($"   Algoritam: {metadata.EncryptionAlgorithm}");
                onMessage?.Invoke($"   Hash algoritam: {metadata.HashAlgorithm}");
                onMessage?.Invoke($"   Hash: {metadata.FileHash.Substring(0, 16)}...");
                onMessage?.Invoke("==================");

                Console.WriteLine($"\n📥 PRIMALAC: Dobijena metadata:");
                Console.WriteLine($"   Datoteka: {metadata.Filename}");
                Console.WriteLine($"   Algoritam: {metadata.EncryptionAlgorithm}");

                using (MemoryStream ms = new MemoryStream())
                {
                    int totalBytes = 0;
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, bytesRead);
                        totalBytes += bytesRead;
                    }

                    byte[] encryptedData = ms.ToArray();
                    onMessage?.Invoke($"📦 Primljeno {totalBytes} bajtova");
                    Console.WriteLine($"   Primljeno {totalBytes} bajtova");

                    string receivedHash = Hashing.TigerHash.ComputeHash(encryptedData);
                    if (receivedHash != metadata.FileHash)
                    {
                        onMessage?.Invoke("❌ HEŠ MISMATCH!");
                        Console.WriteLine("❌ HEŠ MISMATCH!");
                        stream.Close();
                        return null;
                    }

                    onMessage?.Invoke("✅ Heš verifikovan!");
                    Console.WriteLine("✅ Heš verifikovan!");

                    try
                    {
                        byte[] decryptedData = CryptoHelper.DecryptData(encryptedData, metadata.EncryptionAlgorithm);

                        // ← OVDE JE PROMENA: čuvaj u received folder
                        string receivedPath = Path.Combine(receivedDir, metadata.Filename);
                        FileHandler.WriteFile(receivedPath, decryptedData);

                        onMessage?.Invoke($"🎉 USPEŠNO! Dekriptovano: {receivedPath}");
                        Console.WriteLine($"\n🎉 USPEŠNO! Datoteka dekriptovana:");
                        Console.WriteLine($"   {receivedPath}");

                        return metadataJson; // Vrati metadata JSON
                    }
                    catch (Exception ex)
                    {
                        onMessage?.Invoke($"❌ Greška pri dekriptovanju: {ex.Message}");
                        Console.WriteLine($"❌ Greška pri dekriptovanju: {ex.Message}");
                    }
                }

                stream.Close();
                client.Close();
            }
            finally
            {
                listener.Stop();
                onMessage?.Invoke("TCP Server zatvoren");
                Logger.Log("TCP Server zatvoren");
            }

            return null;
        }
    }
}
