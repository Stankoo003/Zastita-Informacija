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
            else if (algorithm == "XXTEA+CBC")
            {
                return CBCMode.Decrypt(encryptedData, EncryptionKey, EncryptionIV);
            }
            else
            {
                throw new System.ArgumentException($"Nepoznat algoritam: {algorithm}");
            }
        }
    }
}
