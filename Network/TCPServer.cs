using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CryptoHelperNamespace;  // DODAJ OVO
using FileOps;
using Logging;

namespace Network
{
    public class TCPServer
    {
        public static void StartServer(int port = 5000)
        {
            Logger.Log($"TCP Server pokrenut na portu {port}");
            Console.WriteLine($"🟢 Server sluša na portu {port}...");
            Console.WriteLine("Čekam klijenta za razmenu datoteka...");

            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            try
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("✅ Klijent se povezao!");

                NetworkStream stream = client.GetStream();
                
                byte[] buffer = new byte[4096];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string metadataJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var metadata = MetadataHandler.ReadMetadata(metadataJson);

                Console.WriteLine($"\n📥 PRIMAОC: Dobijena metadata:");
                Console.WriteLine($"   Datoteka: {metadata.Filename}");
                Console.WriteLine($"   Algoritam: {metadata.EncryptionAlgorithm}");

                using (MemoryStream ms = new MemoryStream())
                {
                    int totalBytes = 0;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, bytesRead);
                        totalBytes += bytesRead;
                    }

                    byte[] encryptedData = ms.ToArray();
                    Console.WriteLine($"   Primetio {totalBytes} bajtova");

                    string receivedHash = Hashing.TigerHash.ComputeHash(encryptedData);
                    if (receivedHash != metadata.FileHash)
                    {
                        Console.WriteLine("❌ HEŠ MISMATCH!");
                        stream.Close();
                        return;
                    }

                    Console.WriteLine("✅ Heš verifikovan!");

                    // KORISTI CRYPTOHELPER
                    byte[] decryptedData = CryptoHelper.DecryptData(encryptedData, metadata.EncryptionAlgorithm);

                    string receivedPath = $"received_{metadata.Filename}";
                    FileHandler.WriteFile(receivedPath, decryptedData);

                    Console.WriteLine($"\n🎉 USPEŠNO! Datoteka dekriptovana:");
                    Console.WriteLine($"   {receivedPath}");
                }

                stream.Close();
                client.Close();
            }
            finally
            {
                listener.Stop();
            }

            Logger.Log("TCP Server zatvoren");
        }
    }
}
