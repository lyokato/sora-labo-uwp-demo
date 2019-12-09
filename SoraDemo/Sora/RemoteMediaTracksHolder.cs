using System;
using System.Collections.Generic;
using System.Linq;

using Org.WebRtc;

namespace Sora
{
    public sealed class RemoteMediaTracksHolder : IDisposable
    {
        public Dictionary<string, IMediaStreamTrack> videoTracks = 
            new Dictionary<string, IMediaStreamTrack>();

        public Dictionary<string, IMediaStreamTrack> audioTracks = 
            new Dictionary<string, IMediaStreamTrack>();

        public bool AddTrack(IMediaStreamTrack track)
        {
            if (track.Kind == "audio" && !audioTracks.ContainsKey(track.Id))
            {
                audioTracks[track.Id] = track;
                return true;
            }

            if (track.Kind == "video" && !videoTracks.ContainsKey(track.Id))
            {
                videoTracks[track.Id] = track;
                return true;
            }
            return false;
        }

        public void RemoveTrack(string kind, string trackId)
        {
            if (kind == "audio")
            {
                if (audioTracks.ContainsKey(trackId))
                {
                    (audioTracks[trackId] as IDisposable)?.Dispose();
                    audioTracks.Remove(trackId);
                }
            }

            if (kind == "video")
            {
                if (videoTracks.ContainsKey(trackId))
                {
                    (videoTracks[trackId] as IDisposable)?.Dispose();
                    videoTracks.Remove(trackId);
                }
            }
        }

        public void Dispose()
        {
            videoTracks.Values.ToList().ForEach((track)=> {
                (track as IDisposable)?.Dispose();
            });
            videoTracks.Clear();

            audioTracks.Values.ToList().ForEach((track)=> {
                (track as IDisposable)?.Dispose();
            });
            audioTracks.Clear();
        }
    }

}
