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
            if (algorithm == "Railfence")
            {
                string encryptedText = System.Text.Encoding.UTF8.GetString(encryptedData);
                string decryptedText = RailfenceCipher.Decrypt(encryptedText, 3);
                return System.Text.Encoding.UTF8.GetBytes(decryptedText);
            }
            else if (algorithm == "XXTEA+CBC" || algorithm == "XXTEA-CBC") // ← Dodaj ovu proveru
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
            if (algorithm == "Railfence")
            {
                string text = System.Text.Encoding.UTF8.GetString(data);
                string encrypted = RailfenceCipher.Encrypt(text, 3);
                return System.Text.Encoding.UTF8.GetBytes(encrypted);
            }
            else if (algorithm == "XXTEA-CBC")
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
