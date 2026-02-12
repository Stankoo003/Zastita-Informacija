using System;
using System.IO;
using System.Security.Cryptography;

namespace CryptoHelperNamespace
{
    public static class KeyManager
    {
        private const string KEY_FILE = "shared.key";

        
        public static byte[] GenerateXXTEAKey()
        {
            byte[] key = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        
        public static byte[] GenerateRandomIV(int size = 16)
        {
            byte[] iv = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }
            return iv;
        }

        
        public static void SaveKey(byte[] key, string filename = KEY_FILE)
        {
            File.WriteAllBytes(filename, key);
            Console.WriteLine($"💾 Ključ sačuvan: {filename}");
        }

        
        public static byte[]? LoadKey(string filename = KEY_FILE)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine($"⚠️ Fajl ne postoji: {filename}");
                return null;
            }

            byte[] key = File.ReadAllBytes(filename);
            if (key.Length != 16)
            {
                Console.WriteLine($"❌ Ključ mora biti 16 bajtova! Učitano: {key.Length}");
                return null;
            }

            Console.WriteLine($"✅ Ključ učitan: {filename}");
            return key;
        }

        
        public static void ApplyKey(byte[] key)
        {
            CryptoHelper.EncryptionKey = key;
            Console.WriteLine("🔑 Ključ primenjen!");
        }
    }
}
