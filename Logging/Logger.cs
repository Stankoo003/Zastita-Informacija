using System;
using System.IO;

namespace Logging
{
    public class Logger
    {
        private static string logFile = "activity.log";

        public static void Log(string message)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(logMessage);
            
            try
            {
                File.AppendAllText(logFile, logMessage + Environment.NewLine);
            }
            catch { }
        }
    }
}
