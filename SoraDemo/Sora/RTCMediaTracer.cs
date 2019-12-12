using System;
using Org.WebRtc;

namespace Sora
{
    public class RTCMediaTracer
    {
        public static void Start()
        {
            var now = DateTime.Now;
            var filename = $"{now.Year}_{now.Month}_{now.Day}_{now.Hour}_{now.Minute}.log";
            var path = Windows.Storage.ApplicationData.Current.LocalFolder.Path + @"\" + filename;
            WebRtcLib.StartMediaTrace(path);
            Logger.Debug("RTCMediaTracer", $"started media trace on: ${path}");
        }

        public static void Stop()
        {
            WebRtcLib.StopMediaTracing();
            Logger.Debug("RTCMediaTracer", $"stopped media trace");
        }
    }
}
