using System;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Media.Capture;

namespace Sora.Device
{
    public class CapturePermission
    {
        public static IAsyncOperation<bool> Request(bool audio, bool video)
        {
            var settings = new MediaCaptureInitializationSettings() {
                AudioDeviceId = "",
                VideoDeviceId = "",
            };
            if (audio && video)
            {
                settings.StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo;
                settings.PhotoCaptureSource = PhotoCaptureSource.VideoPreview;
            }
            else if (audio)
            {
                settings.StreamingCaptureMode = StreamingCaptureMode.Audio;
            }
            else if (video)
            {
                settings.StreamingCaptureMode = StreamingCaptureMode.Video;
                settings.PhotoCaptureSource = PhotoCaptureSource.VideoPreview;
            }
            else
            {
                Logger.Debug("CapturePermission", "no need to request permission");
            }

            var requestor = new MediaCapture();
            Task initTask = requestor.InitializeAsync(settings).AsTask();
            return initTask.ContinueWith(initResult => {

                bool accepted = true;
                if (initResult.Exception != null)
                {
                    Logger.Debug("CapturePermission", "failed to obtain permission");
                    accepted = false;
                }
                return accepted;

            }).AsAsyncOperation<bool>();
        }
    }
}
