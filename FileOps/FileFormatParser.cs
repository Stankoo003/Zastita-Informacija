using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace FileOps
{
    public static class FileFormatParser
    {
        
        public static (byte[] iv, byte[] encryptedData, FileMetaData? metadata) ParseCryptFile(byte[] fileData)
        {
            Console.WriteLine($"\n🔍 Parsing .crypt file ({fileData.Length} bytes)...");

            
            if (fileData.Length >= 16)
            {
                byte[] iv = new byte[16];
                Array.Copy(fileData, 0, iv, 0, 16);

                byte[] encryptedData = new byte[fileData.Length - 16];
                Array.Copy(fileData, 16, encryptedData, 0, encryptedData.Length);

                Console.WriteLine($"   Format 1: IV (16) + Data ({encryptedData.Length})");
                Console.WriteLine($"   IV: {BitConverter.ToString(iv).Substring(0, 47)}...");

                
                if (encryptedData.Length % 16 == 0)
                {
                    Console.WriteLine("   ✅ Deljivo sa 16 - verovatno Format 1");
                    return (iv, encryptedData, null);
                }
                else
                {
                    Console.WriteLine("   ⚠️ NIJE deljivo sa 16 - može biti Format 2 ili 3");
                }
            }

            
            
            try
            {
                byte[] iv = new byte[16];
                Array.Copy(fileData, 0, iv, 0, 16);

                
                for (int i = 16; i < Math.Min(fileData.Length, 1024); i++)
                {
                    if (fileData[i] == '}')
                    {
                        
                        int metadataLength = i - 16 + 1;
                        byte[] metadataBytes = new byte[metadataLength];
                        Array.Copy(fileData, 16, metadataBytes, 0, metadataLength);

                        string metadataJson = Encoding.UTF8.GetString(metadataBytes);
                        
                        try
                        {
                            var metadata = JsonSerializer.Deserialize<FileMetaData>(metadataJson);
                            
                            if (metadata != null && !string.IsNullOrEmpty(metadata.FileName))
                            {
                                
                                int encryptedDataOffset = 16 + metadataLength;
                                byte[] encryptedData = new byte[fileData.Length - encryptedDataOffset];
                                Array.Copy(fileData, encryptedDataOffset, encryptedData, 0, encryptedData.Length);

                                Console.WriteLine($"   Format 2: IV (16) + Metadata ({metadataLength}) + Data ({encryptedData.Length})");
                                Console.WriteLine($"   Metadata: {metadataJson.Substring(0, Math.Min(50, metadataJson.Length))}...");
                                Console.WriteLine($"   ✅ JSON parsiran uspešno!");

                                return (iv, encryptedData, metadata);
                            }
                        }
                        catch
                        {
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Format 2 parsing failed: {ex.Message}");
            }

            
            try
            {
                if (fileData.Length >= 4)
                {
                    int metadataLength = BitConverter.ToInt32(fileData, 0);
                    
                    if (metadataLength > 0 && metadataLength < 1024 && fileData.Length >= 4 + metadataLength + 16)
                    {
                        byte[] metadataBytes = new byte[metadataLength];
                        Array.Copy(fileData, 4, metadataBytes, 0, metadataLength);

                        string metadataJson = Encoding.UTF8.GetString(metadataBytes);
                        var metadata = JsonSerializer.Deserialize<FileMetaData>(metadataJson);

                        if (metadata != null)
                        {
                            byte[] iv = new byte[16];
                            Array.Copy(fileData, 4 + metadataLength, iv, 0, 16);

                            byte[] encryptedData = new byte[fileData.Length - 4 - metadataLength - 16];
                            Array.Copy(fileData, 4 + metadataLength + 16, encryptedData, 0, encryptedData.Length);

                            Console.WriteLine($"   Format 3: Length (4) + Metadata ({metadataLength}) + IV (16) + Data ({encryptedData.Length})");
                            Console.WriteLine($"   ✅ Parsiran Format 3!");

                            return (iv, encryptedData, metadata);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Format 3 parsing failed: {ex.Message}");
            }

            
            Console.WriteLine("   ⚠️ Koristim fallback Format 1 (samo IV + Data)");
            byte[] fallbackIv = new byte[16];
            Array.Copy(fileData, 0, fallbackIv, 0, 16);

            byte[] fallbackData = new byte[fileData.Length - 16];
            Array.Copy(fileData, 16, fallbackData, 0, fallbackData.Length);

            return (fallbackIv, fallbackData, null);
        }
    }
}
