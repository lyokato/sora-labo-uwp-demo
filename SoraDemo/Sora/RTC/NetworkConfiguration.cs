using System.Linq;

using Org.WebRtc;

namespace Sora.RTC
{
    internal class NetworkConfiguration
    {
        public static RTCConfiguration Create(Signaling.OfferConfig serverConfig, 
            MediaOption mediaOption)
        {
            var iceServers = 
                serverConfig.IceServers.Select(iceServer => new RTCIceServer() {
                    Urls       = iceServer.Urls,
                    Username   = iceServer.Username,
                    Credential = iceServer.Credential,
                }).ToList();

            var conf = new RTCConfiguration()
            {
                BundlePolicy             = RTCBundlePolicy.MaxBundle,
                IceTransportPolicy       = RTCIceTransportPolicy.All,
                IceServers               = iceServers,
                RtcpMuxPolicy            = RTCRtcpMuxPolicy.Require,
                ContinualGatheringPolicy = RTCContinualGatheringPolicy.Continually,
                EnableDtlsSrtp           = true,
                SdpSemantics             = RTCSdpSemantics.UnifiedPlan,
                TcpCandidatePolicy       = mediaOption.TcpCandidatePolicy,
                EnableIceRenomination    = true,
            };

            if (serverConfig.IceTransportPolicy == "relay")
            {
                conf.IceTransportPolicy = RTCIceTransportPolicy.Relay;
            }

            return conf;
        }

    }
}
