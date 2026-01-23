using System;

namespace Ciphers{
public class CBCMode
{
    public static byte[] Encrypt(byte[] plaintext, byte[] key, byte[] iv)
    {
        if (iv.Length != 8)
            throw new ArgumentException("IV mora biti 8 bajtova");

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] previousBlock = (byte[])iv.Clone();

        // Proces: svaki blok se XOR-a sa prethodnim šifrovnim blokom
        for (int i = 0; i < plaintext.Length; i += 8)
        {
            int blockSize = Math.Min(8, plaintext.Length - i);
            byte[] block = new byte[8];
            Array.Copy(plaintext, i, block, 0, blockSize);

            // XOR sa prethodnim blokom
            for (int j = 0; j < 8; j++)
                block[j] ^= previousBlock[j];

            // Enkriptuj blok
            byte[] encrypted = XXTEACipher.Encrypt(block, key);
            Array.Copy(encrypted, 0, ciphertext, i, blockSize);

            previousBlock = encrypted;
        }

        return ciphertext;
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] key, byte[] iv)
    {
        if (iv.Length != 8)
            throw new ArgumentException("IV mora biti 8 bajtova");

        byte[] plaintext = new byte[ciphertext.Length];
        byte[] previousBlock = (byte[])iv.Clone();

        for (int i = 0; i < ciphertext.Length; i += 8)
        {
            int blockSize = Math.Min(8, ciphertext.Length - i);
            byte[] block = new byte[blockSize];
            Array.Copy(ciphertext, i, block, 0, blockSize);

            byte[] encrypted = new byte[8];
            Array.Copy(block, encrypted, blockSize);

            // Dekriptuj blok
            byte[] decrypted = XXTEACipher.Decrypt(encrypted, key);

            // XOR sa prethodnim šifrovnim blokom
            for (int j = 0; j < blockSize; j++)
                plaintext[i + j] = (byte)(decrypted[j] ^ previousBlock[j]);

            previousBlock = encrypted;
        }

        return plaintext;
    }
}
}