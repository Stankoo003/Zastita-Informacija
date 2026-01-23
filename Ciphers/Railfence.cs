using System;
using System.Text;

namespace Ciphers{
public class RailfenceCipher
{
    public static string Encrypt(string text, int rails = 3)
    {
        if (rails == 1) return text;
        
        var fence = new StringBuilder[rails];
        for (int i = 0; i < rails; i++)
            fence[i] = new StringBuilder();

        int rail = 0;
        int direction = 1; // 1 za dolje, -1 za gore

        foreach (char c in text)
        {
            fence[rail].Append(c);
            
            if (rail == 0)
                direction = 1;
            else if (rail == rails - 1)
                direction = -1;

            rail += direction;
        }

        StringBuilder encrypted = new StringBuilder();
        foreach (var r in fence)
            encrypted.Append(r);

        return encrypted.ToString();
    }

    public static string Decrypt(string text, int rails = 3)
    {
        if (rails == 1) return text;
        
        int[] railLengths = new int[rails];
        int rail = 0;
        int direction = 1;

        // Izračunaj dužinu svakog šina
        foreach (char c in text)
        {
            railLengths[rail]++;
            if (rail == 0) direction = 1;
            else if (rail == rails - 1) direction = -1;
            rail += direction;
        }

        // Kreiraj šine sa karakterima
        var fence = new Queue<char>[rails];
        int index = 0;
        for (int i = 0; i < rails; i++)
        {
            fence[i] = new Queue<char>();
            for (int j = 0; j < railLengths[i]; j++)
                fence[i].Enqueue(text[index++]);
        }

        // Rekonstruiši tekst
        StringBuilder decrypted = new StringBuilder();
        rail = 0;
        direction = 1;
        for (int i = 0; i < text.Length; i++)
        {
            decrypted.Append(fence[rail].Dequeue());
            if (rail == 0) direction = 1;
            else if (rail == rails - 1) direction = -1;
            rail += direction;
        }

        return decrypted.ToString();
    }
}
}