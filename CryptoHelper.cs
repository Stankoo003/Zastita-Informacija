using CryptoHelperNamespace.Ciphers;
using Hashing;

namespace CryptoHelperNamespace
{
    public static class CryptoHelper
    {
        public static byte[] EncryptionKey { get; set; } = null!;
        public static byte[] EncryptionIV { get; set; } = null!;

        public static byte[] EncryptData(byte[] data, string algorithm)
        {
            Console.WriteLine($"\n[ENCRYPT] Algoritam: {algorithm}");
            Console.WriteLine($"[ENCRYPT] Key: {BitConverter.ToString(EncryptionKey).Substring(0, 47)}...");

            if (algorithm == "Railfence")
            {
                
                string text = System.Text.Encoding.UTF8.GetString(data);
                var railfence = new RailFenceCipher(3);
                byte[] encryptedBytes = railfence.Encrypt(System.Text.Encoding.UTF8.GetBytes(text));
                return encryptedBytes;
            }
            else if (algorithm == "XXTEA-CBC" || algorithm == "XXTEA+CBC")
            {
                var xxtea = new XXTEA(EncryptionKey);
                var cbc = new CBC(xxtea, null);
                return cbc.Encrypt(data);
            }
            else
            {
                throw new ArgumentException($"Nepoznat algoritam: {algorithm}");
            }
        }

        public static byte[] DecryptData(byte[] encryptedData, string algorithm)
        {
            Console.WriteLine($"\n[DECRYPT] Algoritam: {algorithm}");
            Console.WriteLine($"[DECRYPT] Key: {BitConverter.ToString(EncryptionKey).Substring(0, 47)}...");

            if (algorithm == "Railfence")
            {
                
                var railfence = new RailFenceCipher(3);
                byte[] decryptedBytes = railfence.Decrypt(encryptedData);
                return decryptedBytes;
            }
            else if (algorithm == "XXTEA-CBC" || algorithm == "XXTEA+CBC")
            {
                var xxtea = new XXTEA(EncryptionKey);
                var cbc = new CBC(xxtea);  
                return cbc.Encrypt(encryptedData);
            }
            else
            {
                throw new ArgumentException($"Nepoznat algoritam: {algorithm}");
            }
        }

        
        public static void SaveEncryptedFile(string filename, byte[] data, string algorithm)
        {
            string encryptedDir = "encrypted";
            Directory.CreateDirectory(encryptedDir);

            string encryptedPath = Path.Combine(encryptedDir, Path.GetFileName(filename) + ".enc");
            string metadataPath = encryptedPath + ".meta";

            File.WriteAllBytes(encryptedPath, data);

            var tigerHash = new Hashing.TigerHash();
            string hash = tigerHash.ComputeHash(data);
            string metadata = FileOps.MetadataHandler.CreateMetadata(filename, data, algorithm, "Tiger", hash);
            File.WriteAllText(metadataPath, metadata);

            Console.WriteLine($"💾 Sačuvano: {encryptedPath}");
        }

        public static void SaveDecryptedFile(string encryptedFilename, byte[] data)
{
    string decryptedDir = "decrypted";
    Directory.CreateDirectory(decryptedDir);
    string originalName;   
    if (encryptedFilename.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
    {
        originalName = encryptedFilename.Substring(0, encryptedFilename.Length - 4);
    }
    else if (encryptedFilename.EndsWith(".crypt", StringComparison.OrdinalIgnoreCase))
    {
        originalName = encryptedFilename.Substring(0, encryptedFilename.Length - 6);
    }
    else
    {
        originalName = encryptedFilename;
    }
    
    string decryptedPath = Path.Combine(decryptedDir, originalName);
    File.WriteAllBytes(decryptedPath, data);
    Console.WriteLine($"💾 Dekriptovano: {decryptedPath}");
}

    }
}
