using System;

namespace Sora
{
    public class Logger
    {
        public static bool Enabled { get; set; }

        public static void Debug(string component, string msg)
        {
            if (Enabled)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now} Sora<{component}> {msg}");    
            }
        }

    }
}
