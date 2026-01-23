using System;
using System.Net.Sockets;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Ovo radi na Mac-u!");
        
        // TCP test
        TcpListener listener = new TcpListener(System.Net.IPAddress.Loopback, 5000);
        Console.WriteLine("TCP je dostupan!");
        
        // File operations
        File.WriteAllText("test.txt", "Hello Mac!");
        Console.WriteLine("File I/O je dostupan!");
    }
}
