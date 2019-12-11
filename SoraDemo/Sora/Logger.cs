using System;
using Org.WebRtc;

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

        public static void StartMediaTrace()
        {
            if (Enabled)
            {
                var now = DateTime.Now;
                var filename = $"{now.Year}_{now.Month}_{now.Day}_{now.Hour}_{now.Minute}.log";
                var path = Windows.Storage.ApplicationData.Current.LocalFolder.Path + @"\" + filename;
                WebRtcLib.StartMediaTrace(path);
                Debug("Logger", $"started media trace on: ${path}");
            }
        }

        public static void StopMediaTrace()
        {
            if (Enabled)
            {
                WebRtcLib.StopMediaTracing();
                Debug("Logger", $"stopped media trace");
            }
        }
    }
}
