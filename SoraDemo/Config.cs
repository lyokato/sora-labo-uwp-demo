using System.Runtime.Serialization;

namespace SoraDemo
{
    public class Config
    {
        public static readonly string SoraLaboEndpoint = "wss://sora-labo.shiguredo.jp/signaling";
        public static readonly string SoraLaboSignalingKey = "";
        public static readonly string SoraLaboUsername = "";
    }

}

namespace Sora.Signaling
{
    public partial class Metadata
    {
        [DataMember(Name = "signaling_key")]
        public string SignalingKey { get; set; } = "";
    }

}
