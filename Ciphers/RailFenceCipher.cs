using System;

namespace CryptoHelperNamespace.Ciphers
{
    public class RailFenceCipher
    {
        private readonly int railsKey;

        public RailFenceCipher(int rails = 3)
        {
            this.railsKey = rails;
        }

        public string Name => "RailFenceCipher";

        public byte[] Encrypt(byte[] data)
        {
            if (data == null || data.Length == 0)
                return Array.Empty<byte>();

            int[] pattern = GetRailPattern(data.Length, this.railsKey);
            byte[] result = new byte[data.Length];
            int k = 0;

            for (int j = 0; j < this.railsKey; j++)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (pattern[i] == j)
                    {
                        result[k++] = data[i];
                    }
                }
            }
            return result;
        }

        public byte[] Decrypt(byte[] data)
        {
            if (data == null || data.Length == 0)
                return Array.Empty<byte>();

            int[] pattern = GetRailPattern(data.Length, this.railsKey);
            byte[] result = new byte[data.Length];
            int k = 0;

            byte[,] railMatrix = new byte[railsKey, data.Length];

            for (int j = 0; j < railsKey; j++)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (pattern[i] == j)
                    {
                        railMatrix[j, i] = data[k++];
                    }
                }
            }

            for (int i = 0; i < data.Length; i++)
            {
                result[i] = railMatrix[pattern[i], i];
            }

            return result;
        }

        private int[] GetRailPattern(int datalength, int railsKey)
        {
            int[] pattern = new int[datalength];
            int row = 0;
            bool dirDown = false;

            for (int i = 0; i < datalength; i++)
            {
                pattern[i] = row;
                if (row == 0 || row == railsKey - 1)
                    dirDown = !dirDown;
                if (dirDown)
                    row++;
                else
                    row--;
            }
            return pattern;
        }
    }
}
