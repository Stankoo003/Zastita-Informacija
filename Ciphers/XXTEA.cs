using System;

namespace Ciphers
{
    public class XXTEACipher
    {
        private const uint DELTA = 0x9E3779B9;

        public static byte[] Encrypt(byte[] data, byte[] key)
        {
            if (key.Length != 16)
                throw new ArgumentException("Ključ mora biti 128-bitni (16 bajtova)");

            byte[] encrypted = new byte[data.Length];
            Array.Copy(data, encrypted, data.Length);

            // Enkriptuj podatke u 64-bitnim blokovima
            for (int i = 0; i < encrypted.Length; i += 8)
            {
                if (i + 8 <= encrypted.Length)
                {
                    EncryptBlock(encrypted, i, key);
                }
            }

            return encrypted;
        }

        public static byte[] Decrypt(byte[] data, byte[] key)
        {
            if (key.Length != 16)
                throw new ArgumentException("Ključ mora biti 128-bitni (16 bajtova)");

            byte[] decrypted = new byte[data.Length];
            Array.Copy(data, decrypted, data.Length);

            // Dekriptuj podatke u 64-bitnim blokovima
            for (int i = 0; i < decrypted.Length; i += 8)
            {
                if (i + 8 <= decrypted.Length)
                {
                    DecryptBlock(decrypted, i, key);
                }
            }

            return decrypted;
        }

        private static void EncryptBlock(byte[] data, int offset, byte[] key)
        {
            uint v0 = BitConverter.ToUInt32(data, offset);
            uint v1 = BitConverter.ToUInt32(data, offset + 4);
            uint sum = 0;
            uint k0 = BitConverter.ToUInt32(key, 0);
            uint k1 = BitConverter.ToUInt32(key, 4);
            uint k2 = BitConverter.ToUInt32(key, 8);
            uint k3 = BitConverter.ToUInt32(key, 12);

            for (int i = 0; i < 32; i++)
            {
                v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + k0);
                sum += DELTA;
                v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + k1);
            }

            Array.Copy(BitConverter.GetBytes(v0), 0, data, offset, 4);
            Array.Copy(BitConverter.GetBytes(v1), 0, data, offset + 4, 4);
        }

        private static void DecryptBlock(byte[] data, int offset, byte[] key)
        {
            uint v0 = BitConverter.ToUInt32(data, offset);
            uint v1 = BitConverter.ToUInt32(data, offset + 4);
            uint sum = 0xC6EF3720; // DELTA * 32
            uint k0 = BitConverter.ToUInt32(key, 0);
            uint k1 = BitConverter.ToUInt32(key, 4);
            uint k2 = BitConverter.ToUInt32(key, 8);
            uint k3 = BitConverter.ToUInt32(key, 12);

            for (int i = 0; i < 32; i++)
            {
                v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + k1);
                sum -= DELTA;
                v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + k0);
            }

            Array.Copy(BitConverter.GetBytes(v0), 0, data, offset, 4);
            Array.Copy(BitConverter.GetBytes(v1), 0, data, offset + 4, 4);
        }
    }
}
