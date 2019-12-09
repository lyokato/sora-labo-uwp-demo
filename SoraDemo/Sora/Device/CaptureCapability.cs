using System;
using System.Collections.Generic;
using Windows.Media.MediaProperties;
using Org.WebRtc;

namespace Sora.Device
{
    public sealed class CaptureCapability
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint FrameRate { get; set; }

        // This is for 1.62, 1.75
        public bool MrcEnabled { get; set; }

        // This is for 1.66
        // public MediaRatio PixelAspectRatio { get; set; }

        // This is not for 1.75
        //public string FullDescription { get; set; }

        public string ResolutionDescription { get; set; }
        public string FrameRateDescription { get; set; }

        public VideoCapturerCreationParameters CreationParameters(WebRtcFactory factory)
        {
            var param = new VideoCapturerCreationParameters();
            param.Name = DeviceName;
            param.Id = DeviceId;
            if (DeviceId.Equals("screen-share"))
            {
                param.Factory = factory;
            }
            param.Format = CreateVideoFormat();
            return param;
        }

        public VideoFormat CreateVideoFormat()
        {
            var format = new VideoFormat();
            format.Width = (int)Width;
            format.Height = (int)Height;
            format.Interval = new TimeSpan(0,0,0,0, (int)(1000 / FrameRate));
            format.Fourcc = 0;
            return format;
        }

        override public string ToString()
        {
            return $"{DeviceName} ({ResolutionDescription}/{FrameRateDescription})";
        }

    }
}
