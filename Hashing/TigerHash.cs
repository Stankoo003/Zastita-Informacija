using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Hashing
{
    public class TigerHash
    {
        public static string ComputeHash(byte[] data)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        public static string ComputeFileHash(string filepath)
        {
            using (SHA1 sha1 = SHA1.Create())
            using (FileStream fs = File.OpenRead(filepath))
            {
                byte[] hash = sha1.ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}
