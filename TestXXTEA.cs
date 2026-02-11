using System;
using System.Text;
using CryptoHelperNamespace.Ciphers;

public class TestXXTEA
{
    public static void TestEncryptDecrypt()
    {
        Console.WriteLine("\n=== TEST XXTEA + CBC (Kao kod koleginice) ===");
        
        // Test podaci
        string originalText = "Ovo je test datoteka za indeks 19370. XXTEA CBC Tiger hash.";
        byte[] original = Encoding.UTF8.GetBytes(originalText);
        
        // Ključ (16 bajtova)
        byte[] key = new byte[16] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 
                                    0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10 };
        
        // IV (16 bajtova za CBC)
        byte[] iv = new byte[16] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, 
                                   0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };
        
        Console.WriteLine($"Original ({original.Length} bytes): {originalText}");
        Console.WriteLine($"Key: {BitConverter.ToString(key)}");
        Console.WriteLine($"IV:  {BitConverter.ToString(iv)}");
        
        try
        {
            // Test CBC enkriptovanje
            var xxtea = new XXTEA(key);
            var cbc = new CBC(xxtea, iv);
            
            byte[] encrypted = cbc.Encrypt(original);
            Console.WriteLine($"\nEnkriptovano CBC ({encrypted.Length} bytes):");
            Console.WriteLine($"Hex (prvih 32 bytes): {BitConverter.ToString(encrypted.Take(32).ToArray())}...");
            
            // Test CBC dekriptovanje
            byte[] decrypted = cbc.Decrypt(encrypted);
            string decryptedText = Encoding.UTF8.GetString(decrypted);
            Console.WriteLine($"\nDekriptovano ({decrypted.Length} bytes): {decryptedText}");
            
            // Provera
            bool match = original.SequenceEqual(decrypted);
            if (match)
            {
                Console.WriteLine("\n✅ XXTEA + CBC RADI ISPRAVNO!");
                Console.WriteLine($"✅ Padding i IV rade savršeno!");
            }
            else
            {
                Console.WriteLine("\n❌ XXTEA + CBC NE RADI!");
                Console.WriteLine($"❌ Original length: {original.Length}, Decrypted length: {decrypted.Length}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ GREŠKA: {ex.Message}");
        }
        
        Console.WriteLine("===========================================\n");
    }
}
