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

            onMessage?.Invoke($"🌐 TCP Server sluša na portu {port}");
            Console.WriteLine($"🟢 Server sluša na portu {port}...");
            Console.WriteLine($"💡 Dostupan na 127.0.0.1:{port}");

            // ← IZMENA: Sluša na svim interfejsima
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            try
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                onMessage?.Invoke("✅ Klijent se povezao!");
                Console.WriteLine("✅ Klijent se povezao!");

                NetworkStream stream = client.GetStream();

                // Primi metadata
                byte[] buffer = new byte[8192];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string metadataJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                // ← IZMENA: Koristi KOMPATIBILNU metadata (njena struktura)
                var metadata = MetadataHandler.ReadCompatibleMetadata(metadataJson);

                // Pošalji metadata info
                onMessage?.Invoke("📋 === METADATA ===");
                onMessage?.Invoke($"   Fajl: {metadata.FileName}");
                onMessage?.Invoke($"   Veličina: {metadata.SizeBytes} bajtova");
                onMessage?.Invoke($"   Datum: {metadata.Created}");
                onMessage?.Invoke($"   Algoritam: {metadata.Algorithm}");
                onMessage?.Invoke($"   Hash algoritam: {metadata.HashAlgorithm}");
                onMessage?.Invoke($"   Hash: {metadata.HashValue.Substring(0, Math.Min(16, metadata.HashValue.Length))}...");
                onMessage?.Invoke("==================");

                Console.WriteLine($"\n📥 PRIMALAC: Dobijena metadata:");
                Console.WriteLine($"   Datoteka: {metadata.FileName}");
                Console.WriteLine($"   Algoritam: {metadata.Algorithm}");

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

                    // Verifikuj heš
                    var tigerHash = new Hashing.TigerHash();
                    string receivedHash = tigerHash.ComputeHash(encryptedData);
                    
                    if (receivedHash != metadata.HashValue)
                    {
                        onMessage?.Invoke("❌ HEŠ MISMATCH!");
                        onMessage?.Invoke($"   Očekivan: {metadata.HashValue.Substring(0, 16)}...");
                        onMessage?.Invoke($"   Dobijen:  {receivedHash.Substring(0, 16)}...");
                        Console.WriteLine("❌ HEŠ MISMATCH!");
                        stream.Close();
                        return null;
                    }

                    onMessage?.Invoke("✅ Heš verifikovan!");
                    Console.WriteLine("✅ Heš verifikovan!");

                    try
                    {
                        // ← IZMENA: Koristi metadata.Algorithm umesto EncryptionAlgorithm
                        byte[] decryptedData = CryptoHelper.DecryptData(encryptedData, metadata.Algorithm);

                        // Sačuvaj u received folder
                        string receivedPath = Path.Combine(receivedDir, metadata.FileName);
                        FileHandler.WriteFile(receivedPath, decryptedData);

                        onMessage?.Invoke($"🎉 USPEŠNO! Dekriptovano: {receivedPath}");
                        Console.WriteLine($"\n🎉 USPEŠNO! Datoteka dekriptovana:");
                        Console.WriteLine($"   {receivedPath}");

                        Logger.Log($"Received and decrypted: {receivedPath}");
                        return metadataJson;
                    }
                    catch (Exception ex)
                    {
                        onMessage?.Invoke($"❌ Greška pri dekriptovanju: {ex.Message}");
                        Console.WriteLine($"❌ Greška pri dekriptovanju: {ex.Message}");
                        Logger.Log($"Decryption error: {ex.Message}");
                    }
                }

                stream.Close();
                client.Close();
            }
            finally
            {
                listener.Stop();
                onMessage?.Invoke("⏹️ TCP Server zatvoren");
                Logger.Log("TCP Server closed");
            }

            return null;
        }
    }
}
