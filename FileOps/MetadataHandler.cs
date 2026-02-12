using System;
using System.IO;
using System.Text.Json;

namespace FileOps
{
    
    public class Metadata
    {
        public string Filename { get; set; } = "";
        public long FileSize { get; set; }
        public string CreatedDate { get; set; } = "";
        public string EncryptionAlgorithm { get; set; } = "";
        public string HashAlgorithm { get; set; } = "";
        public string FileHash { get; set; } = "";
    }

    
    public class FileMetaData
    {
        public string FileName { get; set; } = "";
        public long SizeBytes { get; set; }
        public DateTime Created { get; set; }
        public string Algorithm { get; set; } = "";
        public string HashAlgorithm { get; set; } = "";
        public string HashValue { get; set; } = "";

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = false });
        }
    }

    public static class MetadataHandler
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
            return JsonSerializer.Deserialize<Metadata>(json)!;
        }

        
        public static string CreateCompatibleMetadata(string originalFilename, byte[] fileData, 
            string algorithm, string hashAlgorithm, string hashValue)
        {
            var metadata = new FileMetaData
            {
                FileName = Path.GetFileName(originalFilename),
                SizeBytes = fileData.Length,
                Created = DateTime.Now,
                Algorithm = algorithm,
                HashAlgorithm = hashAlgorithm,
                HashValue = hashValue
            };

            return metadata.ToJson();
        }

        
        public static FileMetaData ReadCompatibleMetadata(string json)
        {
            return JsonSerializer.Deserialize<FileMetaData>(json)!;
        }
    }
}
