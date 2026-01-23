using System;
using System.IO;
using System.Text.Json;

namespace FileOps{
public class Metadata
{
    public string Filename { get; set; }
    public long FileSize { get; set; }
    public string CreatedDate { get; set; }
    public string EncryptionAlgorithm { get; set; }
    public string HashAlgorithm { get; set; }
    public string FileHash { get; set; }
}

public class MetadataHandler
{
    public static string CreateMetadata(string originalFilename, byte[] fileData, 
        string encryptAlgorithm, string hashAlgorithm, string fileHash)
    {
        var metadata = new Metadata
        {
            Filename = Path.GetFileName(originalFilename),
            FileSize = fileData.Length,
            CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            EncryptionAlgorithm = encryptAlgorithm,
            HashAlgorithm = hashAlgorithm,
            FileHash = fileHash
        };

        return JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
    }

    public static Metadata ReadMetadata(string json)
    {
        return JsonSerializer.Deserialize<Metadata>(json);
    }
}
}