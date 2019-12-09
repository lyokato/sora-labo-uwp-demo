using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Org.WebRtc;

namespace Sora
{
    public class Initializer
    {
        public static async Task<bool> Init(
            CoreDispatcher dispatcher, 
            bool sendAudio, 
            bool sendVideo, 
            bool logEnabled = false)
        {
            Logger.Enabled = logEnabled;

            Logger.Debug("Initializer", "Init");

            bool success = await Device.CapturePermission.Request(sendAudio, sendVideo).AsTask();

            if (success)
            {
                Logger.Debug("Initializer", "got permission");
                SetupRTC(dispatcher);
                return true;
            } 

            Logger.Debug("Initializer", "fialed to get permission");
            return false;
        }

        public static void SetupRTC(CoreDispatcher dispatcher)
        {
            Logger.Debug("Initializer", "SetpRTC");
            var queue = EventQueueMaker.Bind(dispatcher);
            var conf = new WebRtcLibConfiguration
            {
                Queue = queue,
                AudioCaptureFrameProcessingQueue = EventQueue.GetOrCreateThreadQueueByName("AudioCaptureProcessingQueue"),
                AudioRenderFrameProcessingQueue = EventQueue.GetOrCreateThreadQueueByName("AudioRenderProcessingQueue"),
                VideoFrameProcessingQueue = EventQueue.GetOrCreateThreadQueueByName("VideoFrameProcessingQueue"),
                CustomAudioQueue = EventQueue.GetOrCreateThreadQueueByName("CustomAudioQueue"),
                CustomVideoQueue = EventQueue.GetOrCreateThreadQueueByName("CustomVideoQueue")
            };
            WebRtcLib.Setup(conf);
        }
    }
}
