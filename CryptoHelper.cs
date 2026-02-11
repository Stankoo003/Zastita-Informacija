using Ciphers;
using Hashing;

namespace CryptoHelperNamespace
{
    public static class CryptoHelper
    {
        public static byte[] EncryptionKey { get; set; } = null!;
        public static byte[] EncryptionIV { get; set; } = null!;

        public static byte[] DecryptData(byte[] encryptedData, string algorithm)
        {
            Console.WriteLine($"\n[DECRYPT] Algoritam: {algorithm}");
            Console.WriteLine($"[DECRYPT] Key: {BitConverter.ToString(EncryptionKey).Substring(0, 47)}...");
            Console.WriteLine($"[DECRYPT] IV:  {BitConverter.ToString(EncryptionIV)}");

            if (algorithm == "Railfence")
            {
                string encryptedText = System.Text.Encoding.UTF8.GetString(encryptedData);
                string decryptedText = RailfenceCipher.Decrypt(encryptedText, 3);
                return System.Text.Encoding.UTF8.GetBytes(decryptedText);
            }
            else if (algorithm == "XXTEA+CBC" || algorithm == "XXTEA-CBC")
            {
                return CBCMode.Decrypt(encryptedData, EncryptionKey, EncryptionIV);
            }
            else
            {
                throw new System.ArgumentException($"Nepoznat algoritam: {algorithm}");
            }
        }

        public static byte[] EncryptData(byte[] data, string algorithm)
        {
            Console.WriteLine($"\n[ENCRYPT] Algoritam: {algorithm}");
            Console.WriteLine($"[ENCRYPT] Key: {BitConverter.ToString(EncryptionKey).Substring(0, 47)}...");
            Console.WriteLine($"[ENCRYPT] IV:  {BitConverter.ToString(EncryptionIV)}");

            if (algorithm == "Railfence")
            {
                string text = System.Text.Encoding.UTF8.GetString(data);
                string encrypted = RailfenceCipher.Encrypt(text, 3);
                return System.Text.Encoding.UTF8.GetBytes(encrypted);
            }
            else if (algorithm == "XXTEA-CBC" || algorithm == "XXTEA+CBC")
            {
                return CBCMode.Encrypt(data, EncryptionKey, EncryptionIV);
            }
            else
            {
                throw new System.ArgumentException($"Nepoznat algoritam: {algorithm}");
            }
        }


    }
}
