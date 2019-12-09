using System;
using Org.WebRtc;
using Sora.Device;

namespace Sora
{
    public class VideoOption
    {
        public enum Codec
        {
        //    H264, SetLocalDescriptionでしくじるのでいったんはずす
            VP8,
            VP9,
        }

        public static string GetCodecString(Codec codec)
        {
            switch (codec)
            {
         //       case Codec.H264:
         //           return "H264";
                case Codec.VP8:
                    return "VP8";
                case Codec.VP9:
                    return "VP9";
                default:
                    return "VP9";
            }
        }
    }
    
    public class AudioOption
    {
        public enum Codec
        {
            OPUS,
            PCMU,
        }

        public static string GetCodecString(Codec codec)
        {
            switch (codec)
            {
                case Codec.OPUS:
                    return "opus";
                case Codec.PCMU:
                    return "pcmu";
                default:
                    return "opus";
            }
        }
    }

    public sealed class MediaOption
    {
        public RTCTcpCandidatePolicy TcpCandidatePolicy { get; set; } = 
            RTCTcpCandidatePolicy.Enabled;

        public bool AudioDownstreamEnabled {
            get {
                return (SpeakerDevice != null);
            }
        }
        public bool AudioUpstreamEnabled {
            get {
                return (MicrophoneDevice != null);
            }
        } 

        public MediaDevice SpeakerDevice { get; set; } 
        public MediaDevice MicrophoneDevice { get; set; } 

        public string AudioTrackId { get; set; } = 
            Guid.NewGuid().ToString();

        public bool VideoDownstreamEnabled { get; set; } = true;

        public CaptureCapability VideoCaptureCapability {get; set;}

        public bool VideoUpstreamEnabled
        {
            get
            {
                return VideoCaptureCapability != null;
            }
        }

        public string VideoTrackId { get; set; } = 
            Guid.NewGuid().ToString();


        public string StreamId { get; set; } =
            Guid.NewGuid().ToString();

        public bool MultistreamEnabled { get; set; } = true;

        public VideoOption.Codec VideoCodec { get; set; } = VideoOption.Codec.VP9;
        public AudioOption.Codec AudioCodec { get; set; } = AudioOption.Codec.OPUS;

        public int? VideoBitrate = null;

        public bool MultistreamIsRequired()
        {
            if (DownstreamIsRequired() && UpstreamIsRequired())
            {
                return true;
            } else
            {
                return MultistreamEnabled;
            }
        }

        public bool AudioIsRequired()
        {
            return (AudioDownstreamEnabled || AudioUpstreamEnabled);
        }

        public bool VideoIsRequired()
        {
            return (VideoDownstreamEnabled || VideoUpstreamEnabled);
        }

        public bool UpstreamIsRequired()
        {
            return (VideoUpstreamEnabled || AudioUpstreamEnabled);
        }

        public bool DownstreamIsRequired()
        {
            return (VideoDownstreamEnabled || AudioDownstreamEnabled);
        }

        public Role GetRequiredRole()
        {
            if (UpstreamIsRequired())
            {
                return Role.Upstream;
            } else
            {
                return Role.Downstream;
            }
        }

        public bool EnableMRC { get; private set; } = false;

        public void DumpLog()
        {
            Logger.Debug("MediaOption", $"StreamId: {StreamId}");
            Logger.Debug("MediaOption", $"UpstreamIsRequired: {UpstreamIsRequired()}");
            Logger.Debug("MediaOption", $"DownstreamIsRequired: {DownstreamIsRequired()}");
            Logger.Debug("MediaOption", $"MultistreamEnabled: {MultistreamEnabled}");
            Logger.Debug("MediaOption", $"MultistreamIsRequired: {MultistreamIsRequired()}");
            Logger.Debug("MediaOption", $"VideoIsRequired: {VideoIsRequired()}");
            Logger.Debug("MediaOption", $"VideoCodec: {VideoOption.GetCodecString(VideoCodec)}");
            Logger.Debug("MediaOption", $"VideoDownstreamEnabled: {VideoDownstreamEnabled}");
            Logger.Debug("MediaOption", $"VideoUpstreamEnabled: {VideoUpstreamEnabled}");
            Logger.Debug("MediaOption", $"VideoTrackId: {VideoTrackId}");
            Logger.Debug("MediaOption", $"AudioIsRequired: {AudioIsRequired()}");
            Logger.Debug("MediaOption", $"AudioCodec: {AudioOption.GetCodecString(AudioCodec)}");
            Logger.Debug("MediaOption", $"AudioDownstreamEnabled: {AudioDownstreamEnabled}");
            if (AudioDownstreamEnabled)
            {
                Logger.Debug("MediaOption", $"SpeakerDeviceId: {SpeakerDevice.Id}");
                Logger.Debug("MediaOption", $"SpeakerDeviceId: {SpeakerDevice.Name}");
            }
            Logger.Debug("MediaOption", $"AudioUpstreamEnabled: {AudioUpstreamEnabled}");
            if (AudioUpstreamEnabled)
            {
                Logger.Debug("MediaOption", $"MicrophoneDeviceId: {MicrophoneDevice.Id}");
                Logger.Debug("MediaOption", $"MicrophoneDeviceId: {MicrophoneDevice.Name}");
            }
            Logger.Debug("MediaOption", $"AudioTrackId: {AudioTrackId}");
        }
    }
}
