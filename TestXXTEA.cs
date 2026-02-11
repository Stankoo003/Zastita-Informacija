using System;
using System.Text;
using Ciphers;

public class TestXXTEA
{
    public static void TestEncryptDecrypt()
    {
        Console.WriteLine("\n=== TEST XXTEA ===");
        
        // Test podaci
        string originalText = "Ovo je test datoteka za indeks 19370. Railfence XXTEA CBC Tiger hash.";
        byte[] original = Encoding.UTF8.GetBytes(originalText);
        
        // Ključ
        byte[] key = new byte[16] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 
                                    0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10 };
        
        Console.WriteLine($"Original ({original.Length} bytes): {originalText}");
        
        // Test 1: XXTEA direktno (bez CBC) - mora biti tačno 8 bajtova
        byte[] testBlock = new byte[8];
        Array.Copy(original, testBlock, Math.Min(8, original.Length));
        
        Console.WriteLine($"\nTest blok (8 bytes): {Encoding.UTF8.GetString(testBlock)}");
        Console.WriteLine($"Hex: {BitConverter.ToString(testBlock)}");
        
        byte[] encrypted = XXTEACipher.Encrypt(testBlock, key);
        Console.WriteLine($"\nEnkriptovano: {BitConverter.ToString(encrypted)}");
        
        byte[] decrypted = XXTEACipher.Decrypt(encrypted, key);
        Console.WriteLine($"Dekriptovano: {BitConverter.ToString(decrypted)}");
        Console.WriteLine($"Tekst: {Encoding.UTF8.GetString(decrypted)}");
        
        // Provera
        bool match = true;
        for (int i = 0; i < 8; i++)
        {
            if (testBlock[i] != decrypted[i])
            {
                match = false;
                Console.WriteLine($"RAZLIKA na poziciji {i}: {testBlock[i]} != {decrypted[i]}");
            }
        }
        
        if (match)
            Console.WriteLine("\n✅ XXTEA RADI ISPRAVNO!");
        else
            Console.WriteLine("\n❌ XXTEA NE RADI!");
        
        Console.WriteLine("==================\n");
    }
}
