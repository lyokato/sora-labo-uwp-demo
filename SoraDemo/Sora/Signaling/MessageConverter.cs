using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Sora.Signaling
{
    [DataContract]
    public class MessageCommonPart
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }
    }

    [DataContract]
    public class AnswerMessage
    {
        [DataMember(Name = "type")]
        public string Type { get; set; } = "answer";

        [DataMember(Name = "sdp")]
        public string Sdp { get; set; }
    }

    [DataContract]
    public class UpdateMessage
    {
        [DataMember(Name = "type")]
        public string Type { get; set; } = "update";

        [DataMember(Name = "sdp")]
        public string Sdp { get; set; }
    }

    [DataContract]
    public class ReOfferMessage
    {
        [DataMember(Name = "type")]
        public string Type { get; set; } = "re-offer";

        [DataMember(Name = "sdp")]
        public string Sdp { get; set; }
    }

    [DataContract]
    public class ReAnswerMessage
    {
        [DataMember(Name = "type")]
        public string Type { get; set; } = "re-answer";

        [DataMember(Name = "sdp")]
        public string Sdp { get; set; }
    }

    [DataContract]
    public class CandidateMessage
    {
        [DataMember(Name = "type")]
        public string Type { get; set; } = "candidate";

        [DataMember(Name = "candidate")]
        public string Candidate { get; set; }
    }

    [DataContract]
    public class AudioSetting
    {
        [DataMember(Name = "codec_type")]
        public string CodecType { get; set; }
    }

    [DataContract]
    public class VideoSetting
    {
        [DataMember(Name = "codec_type")]
        public string CodecType { get; set; }

        [DataMember(Name = "bit_rate", EmitDefaultValue = false)]
        public int? BitRate { get; set; }
    }

    [DataContract]
    public class ConnectMessage
    {
        [DataMember(Name = "type")]
        public string Type { get; set; } = "connect";

        [DataMember(Name = "role")]
        public string Role { get; set; }

        [DataMember(Name = "channel_id")]
        public string ChannelId { get; set; }

        [DataMember(Name = "metadata")]
        public Metadata Metadata { get; set; }

        [DataMember(Name = "sdp")]
        public string Sdp { get; set; }

        [DataMember(Name = "sdk_type")]
        public string SDKType { get; set; } = SDK.Name;

        [DataMember(Name = "sdk_version")]
        public string SDKVersion { get; set; } = SDK.Version;

        [DataMember(Name = "user_agent")]
        public string UserAgent { get; set; } = Device.VersionInfo.GetInfo();

        [DataMember(Name = "multistream")]
        public bool Multistream { get; set; }
    }

    [DataContract]
    public class ConnectMessageWithoutAudioVideo  : ConnectMessage
    {
        [DataMember(Name = "audio")]
        public bool Audio { get; } = false;

        [DataMember(Name = "video")]
        public bool Video { get; } = false;
    }

    [DataContract]
    public class ConnectMessageWithAudio  : ConnectMessage
    {
        [DataMember(Name = "audio")]
        public AudioSetting Audio { get; set; }

        [DataMember(Name = "video")]
        public bool Video { get; set; } = false;
    }

    [DataContract]
    public class ConnectMessageWithVideo  : ConnectMessage
    {
        [DataMember(Name = "audio")]
        public bool Audio { get; set; } = false;

        [DataMember(Name = "video")]
        public VideoSetting Video { get; set; }
    }

    [DataContract]
    public class ConnectMessageWithAudioVideo  : ConnectMessage
    {
        [DataMember(Name = "audio")]
        public AudioSetting Audio { get; set; }

        [DataMember(Name = "video")]
        public VideoSetting Video { get; set; }
    }

    [DataContract]
    public partial class Metadata 
    {
    }


    [DataContract]
    public class PongMessage
    {
        [DataMember(Name = "type")]
        public string Type { get; set; } = "pong";
    }

    [DataContract]
    public class IceServer
    {
        [DataMember(Name = "urls")]
        public List<string> Urls { get; set; } = new List<string>();

        [DataMember(Name = "credential")]
        public string Credential { get; set; }

        [DataMember(Name = "username")]
        public string Username { get; set; }
    }

    [DataContract]
    public class OfferConfig
    {
        [DataMember(Name = "iceServers")]
        public List<IceServer> IceServers { get; set; } = new List<IceServer>();

        [DataMember(Name = "iceTransportPolicy")]
        public string IceTransportPolicy { get; set; } = "";
    }

    [DataContract]
    public class EncodingSetting
    {
        [DataMember(Name = "rid", EmitDefaultValue = false)]
        public string Rid { get; set; } = "";

        [DataMember(Name = "maxBitrate", EmitDefaultValue = false)]
        public int? MaxBitrate { get; set; }

        [DataMember(Name = "maxFramerate", EmitDefaultValue = false)]
        public int? MaxFrameRate { get; set; }

        [DataMember(Name = "scaleResolutionDownBy", EmitDefaultValue = false)]
        public double? ScaleResolutionDownBy { get; set; }

    }

    [DataContract]
    public class OfferMessage
    {
        [DataMember(Name = "type")]
        public string Type { get; set; } = "offer";

        [DataMember(Name = "sdp")]
        public string Sdp { get; set; }

        [DataMember(Name = "client_id")]
        public string ClientId { get; set; }

        [DataMember(Name = "config")]
        public OfferConfig Config { get; set; }

        //[DataMember(Name = "encodings", EmitDefaultValue = false)]
        //public List<EncodingSetting> Encodings { get; set; }
    }

    class MessageConverter
    {
        public static string ParseType(string json)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(MessageCommonPart));
                var msg = serializer.ReadObject(ms) as MessageCommonPart;
                return msg.Type;
            }
        }

        public static OfferMessage ParseOfferMessage(string json)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(OfferMessage));
                var offer = serializer.ReadObject(ms) as OfferMessage;
                if (offer.Config == null)
                {
                    offer.Config = new OfferConfig();
                }
                return offer;
            }
        }

        public static ReOfferMessage ParseReOfferMessage(string json)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(ReOfferMessage));
                return serializer.ReadObject(ms) as ReOfferMessage;
            }
        }

        public static UpdateMessage ParseUpdateMessage(string json)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(UpdateMessage));
                return serializer.ReadObject(ms) as UpdateMessage;
            }
        }

        public static string BuildAnswerMessage(string sdp)
        {
            var msg = new AnswerMessage()
            {
                Sdp = sdp,
            };

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(AnswerMessage));
                serializer.WriteObject(ms, msg);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static string BuildReAnswerMessage(string sdp)
        {
            var msg = new ReAnswerMessage()
            {
                Sdp = sdp,
            };

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(ReAnswerMessage));
                serializer.WriteObject(ms, msg);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static string BuildUpdateAnswerMessage(string sdp)
        {
            var msg = new UpdateMessage()
            {
                Sdp = sdp,
            };

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(UpdateMessage));
                serializer.WriteObject(ms, msg);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static string BuildCandidateMessage(string candidate)
        {
            var msg = new CandidateMessage()
            {
                Candidate = candidate,
            };

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(CandidateMessage));
                serializer.WriteObject(ms, msg);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static string BuildPongMessage()
        {
            var msg = new PongMessage();

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(PongMessage));
                serializer.WriteObject(ms, msg);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private static AudioSetting BuildAudioSetting(MediaOption mediaOption)
        {
            /// TODO: Opusのセッティング
            return new AudioSetting
            {
                CodecType = mediaOption.AudioCodec.ToString(),
            };
        }

        private static VideoSetting BuildVideoSetting(MediaOption mediaOption)
        {
            var video = new VideoSetting
            {
                CodecType = mediaOption.VideoCodec.ToString(),
                BitRate = mediaOption.VideoBitrate,
            };

            if (mediaOption.VideoBitrate != null)
            {
                video.BitRate = mediaOption.VideoBitrate;
            }

            return video;
        }

        private static string BuildConnectWithAudioVideoMessage(
            Role role, string channelId, 
            MediaOption mediaOption, Metadata metadata, string sdp)
        {
            var msg = new ConnectMessageWithAudioVideo() {
                Role        = GetRoleString(role),
                ChannelId   = channelId,
                Multistream = mediaOption.MultistreamIsRequired(),
                Metadata    = metadata,
                Sdp         = sdp,
                Audio       = BuildAudioSetting(mediaOption),
                Video       = BuildVideoSetting(mediaOption)
            };

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(ConnectMessageWithAudioVideo));
                serializer.WriteObject(ms, msg);
                var result = Encoding.UTF8.GetString(ms.ToArray());
                return result;
            }
        }

        private static string BuildConnectWithoutAudioVideoMessage(
            Role role, string channelId, 
            MediaOption mediaOption, Metadata metadata, string sdp)
        {
            var msg = new ConnectMessageWithoutAudioVideo() {
                Role        = GetRoleString(role),
                ChannelId   = channelId,
                Multistream = mediaOption.MultistreamIsRequired(),
                Metadata    = metadata,
                Sdp         = sdp
            };

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(ConnectMessageWithoutAudioVideo));
                serializer.WriteObject(ms, msg);
                var result = Encoding.UTF8.GetString(ms.ToArray());
                return result;
            }
        }

        private static string BuildConnectWithAudioMessage(
            Role role, string channelId, 
            MediaOption mediaOption, Metadata metadata, string sdp)
        {
            var msg = new ConnectMessageWithAudio() {
                Role        = GetRoleString(role),
                ChannelId   = channelId,
                Multistream = mediaOption.MultistreamIsRequired(),
                Metadata    = metadata,
                Sdp         = sdp,
                Audio       = BuildAudioSetting(mediaOption)
            };

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(ConnectMessageWithAudio));
                serializer.WriteObject(ms, msg);
                var result = Encoding.UTF8.GetString(ms.ToArray());
                return result;
            }
        }

        private static string BuildConnectWithVideoMessage(
            Role role, string channelId, 
            MediaOption mediaOption, Metadata metadata, string sdp)
        {
            var msg = new ConnectMessageWithVideo() {
                Role        = GetRoleString(role),
                ChannelId   = channelId,
                Multistream = mediaOption.MultistreamIsRequired(),
                Metadata    = metadata,
                Sdp         = sdp,
                Video       = BuildVideoSetting(mediaOption)
            };

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(ConnectMessageWithVideo));
                serializer.WriteObject(ms, msg);
                var result = Encoding.UTF8.GetString(ms.ToArray());
                return result;
            }
        }

        public static string BuildConnectMessage(Role role, string channelId, 
            MediaOption mediaOption, Metadata metadata, string sdp)
        {
            // ConnectメッセージのVideoとAudioは、booleanとObjectのvariantになってしまっているため
            // 多少冗長だが、条件を細かくチェックして型を分ける
            if (mediaOption.UpstreamIsRequired())
            {
                if (mediaOption.AudioUpstreamEnabled && mediaOption.VideoUpstreamEnabled)
                {
                    return BuildConnectWithAudioVideoMessage(
                        role, channelId, mediaOption, metadata, sdp);
                }
                else if (mediaOption.AudioUpstreamEnabled)
                {
                    return BuildConnectWithAudioMessage(
                        role, channelId, mediaOption, metadata, sdp);
                }
                else if (mediaOption.VideoUpstreamEnabled)
                {
                    return BuildConnectWithVideoMessage(
                        role, channelId, mediaOption, metadata, sdp);
                }
                else
                {
                    return BuildConnectWithoutAudioVideoMessage(
                        role, channelId, mediaOption, metadata, sdp);
                }
            }
            else
            {
                if (mediaOption.AudioDownstreamEnabled && mediaOption.VideoDownstreamEnabled)
                {
                    return BuildConnectWithAudioVideoMessage(
                        role, channelId, mediaOption, metadata, sdp);

                }
                else if (mediaOption.AudioDownstreamEnabled)
                {
                    return BuildConnectWithAudioMessage(
                        role, channelId, mediaOption, metadata, sdp);

                }
                else if (mediaOption.VideoDownstreamEnabled)
                {
                    return BuildConnectWithVideoMessage(
                        role, channelId, mediaOption, metadata, sdp);

                }
                else
                {
                    return BuildConnectWithoutAudioVideoMessage(
                        role, channelId, mediaOption, metadata, sdp);
                }
            }
        }

        // TODO 適切な場所へ移す
        static string GetRoleString(Role role)
        {
            switch (role)
            {
                case Role.Downstream:
                    return "downstream";
                case Role.Upstream:
                    return "upstream";
                default:
                    return "downstream";
            }
        }

    }
}
