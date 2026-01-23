using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic; // Za Queue

using Ciphers;
using Hashing;
using FileOps;
using Logging;
class Program
{
    // Konstante
    private const string ENCRYPTION_DIR = "encrypted";
    private const string KEY_FILE = "key.txt";
    private const string IV_FILE = "iv.txt";

    // Globalne varijable
    static byte[] encryptionKey = null!;
    static byte[] encryptionIV = null!;

    static void Main()
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║ Zaštita Informacija - Indeks 19370   ║");
        Console.WriteLine("║ Algoritmi: Railfence, XXTEA, CBC      ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        GenerateKeys();
        Directory.CreateDirectory(ENCRYPTION_DIR);
        Directory.CreateDirectory("decrypted");

        bool running = true;
        while (running)
        {
            Console.WriteLine("\n┌─ MENI ─────────────────────┐");
            Console.WriteLine("│ 1. Enkriptuj datoteku     │");
            Console.WriteLine("│ 2. Dekriptuj datoteku     │");
            Console.WriteLine("│ 3. Pošalji preko TCP      │");
            Console.WriteLine("│ 4. Primi preko TCP        │");
            Console.WriteLine("│ 5. Izlaz                  │");
            Console.WriteLine("└────────────────────────────┘");
            Console.Write("Odabir: ");

            string choice = Console.ReadLine() ?? "0";

            switch (choice)
            {
                case "1":
                    EncryptFile();
                    break;
                case "2":
                    DecryptFile();
                    break;
                case "3":
                    Console.Write("Unesite enkriptovanu datoteku (.enc): ");
                    string sendFile = Console.ReadLine() ?? "";
                    Network.TCPClient.SendFile(sendFile);
                    break;
                case "4":
                    Console.Write("Port za slušanje (default 5000): ");
                    string portStr = Console.ReadLine() ?? "5000";
                    if (int.TryParse(portStr, out int port))
                        Network.TCPServer.StartServer(port);
                    else
                        Console.WriteLine("Pogrešan port!");
                    break;
                case "5":
                    running = false;
                    break;
                default:
                    Console.WriteLine("Nepoznata opcija!");
                    break;
            }
        }

        Logger.Log("Aplikacija zatvorena");
    }

static void GenerateKeys()
{
    encryptionKey = new byte[16];
    new Random().NextBytes(encryptionKey);
    CryptoHelperNamespace.CryptoHelper.EncryptionKey = encryptionKey;  
    Logger.Log("Generisan ključ za XXTEA enkripciju");

    encryptionIV = new byte[8];
    new Random().NextBytes(encryptionIV);
    CryptoHelperNamespace.CryptoHelper.EncryptionIV = encryptionIV;   
    Logger.Log("Generisan inicijalizacijski vektor (IV)");
}


    static void EncryptFile()
    {
        Console.Write("\nUnesite putanju do datoteke: ");
        string filepath = Console.ReadLine() ?? "";

        if (!File.Exists(filepath))
        {
            Console.WriteLine("❌ Datoteka ne postoji!");
            return;
        }

        Console.WriteLine("\nOdaberi algoritam enkripcije:");
        Console.WriteLine("1. Railfence Cipher (samo tekst)");
        Console.WriteLine("2. XXTEA + CBC (binarni)");
        Console.Write("Odabir: ");

        string cipherChoice = Console.ReadLine() ?? "0";

        try
        {
            byte[] fileData = FileHandler.ReadFile(filepath);
            byte[] encryptedData = null!;
            string algorithm = "";

            if (cipherChoice == "1")
            {
                string text = Encoding.UTF8.GetString(fileData);
                string encrypted = RailfenceCipher.Encrypt(text, 3);
                encryptedData = Encoding.UTF8.GetBytes(encrypted);
                algorithm = "Railfence";
            }
            else if (cipherChoice == "2")
            {
                encryptedData = CBCMode.Encrypt(fileData, encryptionKey, encryptionIV);
                algorithm = "XXTEA+CBC";
            }
            else
            {
                Console.WriteLine("❌ Pogrešna opcija!");
                return;
            }

            string fileHash = TigerHash.ComputeHash(encryptedData);
            
            string metadata = MetadataHandler.CreateMetadata(
                filepath, 
                fileData, 
                algorithm, 
                "Tiger (SHA1)", 
                fileHash
            );

            string encryptedPath = FileHandler.GetEncryptedFilename(filepath);
            string metadataPath = encryptedPath + ".meta";

            File.WriteAllText(metadataPath, metadata);
            FileHandler.WriteFile(encryptedPath, encryptedData);

            Console.WriteLine($"\n✅ Uspešno enkriptovano!");
            Console.WriteLine($"   Datoteka: {encryptedPath}");
            Console.WriteLine($"   Metadata: {metadataPath}");
            Console.WriteLine($"   Heš: {fileHash.Substring(0, 16)}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Greška: {ex.Message}");
            Logger.Log($"GREŠKA pri enkripciiji: {ex.Message}");
        }
    }

    static void DecryptFile()
    {
        Console.Write("\nUnesite putanju do enkriptovane datoteke (.enc): ");
        string filepath = Console.ReadLine() ?? "";

        if (!File.Exists(filepath))
        {
            Console.WriteLine("❌ Datoteka ne postoji!");
            return;
        }

        string metadataPath = filepath + ".meta";
        if (!File.Exists(metadataPath))
        {
            Console.WriteLine("❌ Metadata datoteka ne postoji!");
            return;
        }

        try
        {
            string metadataJson = File.ReadAllText(metadataPath);
            var metadata = MetadataHandler.ReadMetadata(metadataJson);

            Console.WriteLine($"\n📋 Metadata:");
            Console.WriteLine($"   Original: {metadata.Filename}");
            Console.WriteLine($"   Veličina: {metadata.FileSize} bajtova");
            Console.WriteLine($"   Algoritam: {metadata.EncryptionAlgorithm}");

            byte[] encryptedData = FileHandler.ReadFile(filepath);
            
            string currentHash = TigerHash.ComputeHash(encryptedData);
            if (currentHash != metadata.FileHash)
            {
                Console.WriteLine("❌ UPOZORENJE: Heš se ne slaže!");
            }

            byte[] decryptedData = null!;

            if (metadata.EncryptionAlgorithm == "Railfence")
            {
                string encrypted = Encoding.UTF8.GetString(encryptedData);
                string decrypted = RailfenceCipher.Decrypt(encrypted, 3);
                decryptedData = Encoding.UTF8.GetBytes(decrypted);
            }
            else if (metadata.EncryptionAlgorithm == "XXTEA+CBC")
            {
                decryptedData = CBCMode.Decrypt(encryptedData, encryptionKey, encryptionIV);
            }

            string decryptedPath = Path.Combine("decrypted", metadata.Filename);
            Directory.CreateDirectory("decrypted");
            FileHandler.WriteFile(decryptedPath, decryptedData);

            Console.WriteLine($"\n✅ Uspešno dekriptovano!");
            Console.WriteLine($"   Datoteka: {decryptedPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Greška: {ex.Message}");
            Logger.Log($"GREŠKA pri dekripciiji: {ex.Message}");
        }
    }
}
