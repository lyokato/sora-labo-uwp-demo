using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sora
{
    /// <summary>
    /// MediaTrackがLabelプロパティを持っていないため、
    /// SDPをパースしてマッチングを行うことで、trackIdからstreamIdを取得できるようにする
    /// </summary>
    public class StreamIdChecker
    {
        Regex msidLinePattern = new Regex(@"msid\:([0-9a-fA-F\-]+)\s([0-9a-fA-F\-]+)$");

        Dictionary<string, string> trackStreamPairs = 
            new Dictionary<string, string>();

        public void PickMsidFromSdp(string sdp)
        {
            var lines = sdp.Split("\r\n");
            foreach (var line in lines)
            {
                var matches = msidLinePattern.Matches(line);
                foreach (Match m in matches)
                {
                    var streamId = m.Groups[1].Value;
                    var trackId = m.Groups[2].Value;

                    if (!trackStreamPairs.ContainsKey(trackId))
                    {
                        Logger.Debug("StreamIdChecker", 
                            $"store trackId:{trackId} and streamId:{streamId}");

                        trackStreamPairs.Add(trackId, streamId);
                    }
                }
            }
        }

        public string GetStreamIdForTrackId(string trackId)
        {
            if (trackStreamPairs.ContainsKey(trackId))
            {
                return trackStreamPairs[trackId];
            }
            else
            {
                return string.Empty;
            }
        }
    }

}
