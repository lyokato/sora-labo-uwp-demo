using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Windows.Graphics.Capture;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Foundation;

namespace Sora.Device
{
    public class MediaDevice
    {

        async public static Task<IList<CaptureCapability>> GetAllVideoCapturerCapabilities()
        {
            List<CaptureCapability> results = new List<CaptureCapability>();

            var devices = await GetVideoCaptureDevices();

            foreach (var device in devices)
            {
                var capabilities = await device.GetCpabilities();
                results.AddRange(capabilities);
            }
            return results;
        }

        async public static Task<IList<MediaDevice>> GetMicrophoneDevices()
        {
            var devices = (await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                Windows.Media.Devices.MediaDevice.GetAudioCaptureSelector()));

            IList<MediaDevice> list = new List<MediaDevice>();

            foreach(var deviceInfo in devices)
            {
                list.Add(new MediaDevice() {
                    Id   = deviceInfo.Id,
                    Name = deviceInfo.Name,
                });
            }

            return list;
        }

        async public static Task<IList<MediaDevice>> GetSpeakerDevices()
        {
            var devices = (await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                Windows.Media.Devices.MediaDevice.GetAudioRenderSelector()));

            IList<MediaDevice> list = new List<MediaDevice>();

            foreach(var deviceInfo in devices)
            {
                list.Add(new MediaDevice() {
                    Id   = deviceInfo.Id,
                    Name = deviceInfo.Name,
                });
            }

            return list;
        }

        async public static Task<IList<MediaDevice>> GetVideoCaptureDevices()
        {
            var devices = (await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                Windows.Media.Devices.MediaDevice.GetVideoCaptureSelector()));

            IList<MediaDevice> list = new List<MediaDevice>();

            foreach(var deviceInfo in devices)
            {
                list.Add(new MediaDevice() {
                    Id   = deviceInfo.Id,
                    Name = deviceInfo.Name,
                    //Location = device.Info.EnclosureLocation
                });
            }

            if (GraphicsCaptureSession.IsSupported())
            {
                list.Add(new MediaDevice() {
                    Id   = Screen.DeviceId,
                    Name = Screen.DeviceName
                });
            }

            return list;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        //public EnclosureLocation Location {get; set;}

        public IAsyncOperation<IList<CaptureCapability>> GetCpabilities()
        {
            if (Id.Equals(Screen.DeviceId))
            {
                return GetScreenShareCpabilities();
            }
            else
            {
                return GetNativeCpabilities();
            }
        }

        public IAsyncOperation<IList<CaptureCapability>> GetScreenShareCpabilities()
        {
            var task = new Task<IList<CaptureCapability>>(() => {
                var list = new List<CaptureCapability>();
                list.Add(new CaptureCapability() {
                    DeviceId = Screen.DeviceId,
                    DeviceName = Screen.DeviceName,
                    Width = 640,
                    Height = 480,
                    FrameRate = 30,
                    MrcEnabled = false,
                    FrameRateDescription = "30fps",
                    ResolutionDescription = "640x480"
                });
                list.Add(new CaptureCapability() {
                    DeviceId = Screen.DeviceId,
                    DeviceName = Screen.DeviceName,
                    Width = 800,
                    Height = 600,
                    FrameRate = 30,
                    MrcEnabled = false,
                    FrameRateDescription = "30fps",
                    ResolutionDescription = "800x600"
                });
                list.Add(new CaptureCapability() {
                    DeviceId = Screen.DeviceId,
                    DeviceName = Screen.DeviceName,
                    Width = 1024,
                    Height = 768,
                    FrameRate = 30,
                    MrcEnabled = false,
                    FrameRateDescription = "30fps",
                    ResolutionDescription = "1024x768"
                });
                return list;
            });
            task.Start();
            return task.AsAsyncOperation<IList<CaptureCapability>>();
        }

        public IAsyncOperation<IList<CaptureCapability>> GetNativeCpabilities()
        {
            if (Id == null)
            {
                return null;
            }

            var capture = new MediaCapture();

            var settings = new MediaCaptureInitializationSettings()
            {
                VideoDeviceId = Id,
            };

            Task initTask = capture.InitializeAsync(settings).AsTask();

            return initTask.ContinueWith(initResult => {

                if (initResult.Exception != null)
                {
                    Logger.Debug("MediaDevice", 
                        $"Failed to initialize video device: {initResult.Exception.Message}");
                    return null;
                }

                var dict = new Dictionary<string, CaptureCapability>();

                var props = capture
                    .VideoDeviceController
                    .GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord);

                foreach (VideoEncodingProperties prop in props)
                {
                    var cap = CreateCapability(prop);
                    // 取得できるプロパティは重複することがあるので、
                    // いったんDictionaryに入れて重複キーをはじく
                    dict[cap.ToString()] = cap;
                }

                IList<CaptureCapability> list = dict.Values.ToList();

                return list;

            }).AsAsyncOperation<IList<CaptureCapability>>();

        }

        CaptureCapability CreateCapability(VideoEncodingProperties prop)
        {
            uint frameRate = CalcFrameRate(prop);

            return new CaptureCapability
            {
                DeviceId              = Id,
                DeviceName            = Name,
                Width                 = (uint)prop.Width,
                Height                = (uint)prop.Height,
                FrameRate             = frameRate,
                MrcEnabled            = true,
                FrameRateDescription  = $"{frameRate}fps",
                ResolutionDescription = $"{prop.Width}x{prop.Height}"
            };
        }

        uint CalcFrameRate(VideoEncodingProperties prop)
        {
            return (uint)(prop.FrameRate.Numerator / prop.FrameRate.Denominator);
        }

        override public string ToString()
        {
            return Name;
        }
    }
}
