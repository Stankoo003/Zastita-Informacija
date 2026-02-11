using System;

namespace Ciphers
{
    public class CBCMode
    {
        public static byte[] Encrypt(byte[] plaintext, byte[] key, byte[] iv)
        {
            if (iv.Length != 8)
                throw new ArgumentException("IV mora biti 8 bajtova");

            // Dodaj PKCS7 padding
            int paddingLength = 8 - (plaintext.Length % 8);
            byte[] paddedPlaintext = new byte[plaintext.Length + paddingLength];
            Array.Copy(plaintext, paddedPlaintext, plaintext.Length);

            // Popuni padding bajtove sa vrednošću padding dužine
            for (int i = plaintext.Length; i < paddedPlaintext.Length; i++)
                paddedPlaintext[i] = (byte)paddingLength;

            byte[] ciphertext = new byte[paddedPlaintext.Length];
            byte[] previousBlock = (byte[])iv.Clone();

            // Proces: svaki blok se XOR-a sa prethodnim šifrovnim blokom
            for (int i = 0; i < paddedPlaintext.Length; i += 8)
            {
                byte[] block = new byte[8];
                Array.Copy(paddedPlaintext, i, block, 0, 8);

                // XOR sa prethodnim blokom
                for (int j = 0; j < 8; j++)
                    block[j] ^= previousBlock[j];

                // Enkriptuj blok (uvek pun 8-bajtni blok)
                byte[] encrypted = XXTEACipher.Encrypt(block, key);
                Array.Copy(encrypted, 0, ciphertext, i, 8);

                previousBlock = encrypted;
            }

            return ciphertext;
        }

        public static byte[] Decrypt(byte[] ciphertext, byte[] key, byte[] iv)
        {
            if (iv.Length != 8)
                throw new ArgumentException("IV mora biti 8 bajtova");

            if (ciphertext.Length % 8 != 0)
                throw new ArgumentException("Ciphertext mora biti deljiv sa 8 bajtova");

            byte[] plaintext = new byte[ciphertext.Length];
            byte[] previousBlock = (byte[])iv.Clone();

            for (int i = 0; i < ciphertext.Length; i += 8)
            {
                byte[] block = new byte[8];
                Array.Copy(ciphertext, i, block, 0, 8);

                // Sačuvaj enkriptovan blok PRE dekriptovanja
                byte[] encryptedCopy = (byte[])block.Clone();

                // Dekriptuj blok (uvek pun 8-bajtni blok)
                byte[] decrypted = XXTEACipher.Decrypt(block, key);

                // XOR sa prethodnim šifrovnim blokom
                for (int j = 0; j < 8; j++)
                    plaintext[i + j] = (byte)(decrypted[j] ^ previousBlock[j]);

                previousBlock = encryptedCopy;
            }

            // Ukloni PKCS7 padding
            int paddingLength = plaintext[plaintext.Length - 1];

            // Validacija padding-a
            if (paddingLength > 0 && paddingLength <= 8)
            {
                // Proveri da li su svi padding bajtovi validni
                bool validPadding = true;
                for (int i = plaintext.Length - paddingLength; i < plaintext.Length; i++)
                {
                    if (plaintext[i] != paddingLength)
                    {
                        validPadding = false;
                        break;
                    }
                }

                if (validPadding)
                {
                    byte[] unpaddedPlaintext = new byte[plaintext.Length - paddingLength];
                    Array.Copy(plaintext, unpaddedPlaintext, unpaddedPlaintext.Length);
                    return unpaddedPlaintext;
                }
            }

            // Ako padding nije validan, vrati originalni plaintext
            return plaintext;
        }
    }
}
