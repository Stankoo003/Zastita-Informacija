using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

using Logging;
namespace Network
{
    public class TCPClient
    {
        public static void SendFile(string encryptedFilepath, string ipAddress = "127.0.0.1", int port = 5000)
        {
            if (!File.Exists(encryptedFilepath))
            {
                Console.WriteLine("❌ Enkriptovana datoteka ne postoji!");
                return;
            }

            string metadataPath = encryptedFilepath + ".meta";
            if (!File.Exists(metadataPath))
            {
                Console.WriteLine("❌ Metadata datoteka ne postoji!");
                return;
            }

            try
            {
                TcpClient client = new TcpClient(ipAddress, port);
                NetworkStream stream = client.GetStream();

                Logger.Log($"Povezivanje na {ipAddress}:{port}...");

                // Pošalji metadata
                string metadataJson = File.ReadAllText(metadataPath);
                byte[] metadataBytes = Encoding.UTF8.GetBytes(metadataJson);
                stream.Write(metadataBytes, 0, metadataBytes.Length);
                Console.WriteLine("📤 Metadata poslat");

                // VAŽNO: Kratak delay da server obradi metadata
                System.Threading.Thread.Sleep(100);

                // Pošalji enkriptovanu datoteku
                byte[] fileData = File.ReadAllBytes(encryptedFilepath);
                stream.Write(fileData, 0, fileData.Length);
                Console.WriteLine($"📤 Poslat {fileData.Length} bajtova");

                stream.Close();
                client.Close();

                Console.WriteLine("\n✅ Datoteka uspešno poslana serveru!");
                Logger.Log($"Datoteka poslana: {encryptedFilepath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Greška pri slanju: {ex.Message}");
                Logger.Log($"TCP Send greška: {ex.Message}");
            }
        }
    }
}
