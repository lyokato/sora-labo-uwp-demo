using System;
using System.Threading;
using System.Threading.Tasks;

using Windows.UI.Core;
using Windows.ApplicationModel.Core;

using Org.WebRtc;

using Sora.RTC;
using Sora.Signaling;

namespace Sora
{
    public class MediaChannel
    {
        public event Action<IMediaStreamTrack> OnAddLocalAudioTrack;
        public event Action<IVideoCapturer, IMediaStreamTrack> OnAddLocalVideoTrack;
        public event Action<IMediaStreamTrack> OnAddRemoteVideoTrack;
        public event Action<string> OnRemoveRemoteVideoTrack;
        public event Action<IMediaStreamTrack> OnAddRemoteAudioTrack;
        public event Action<string> OnRemoveRemoteAudioTrack;
        public event Action OnConnect;
        public event Action OnDisconnect;
        public event Action<ErrorType> OnError;

        SignalingChannel signaling = null;
        PeerChannel peer = null;
        PeerChannel tempPeer = null;
        Timer timer = null;
        bool closed = false;

        string clientId = string.Empty;

        readonly string signalingEndpoint;
        readonly string channelId;
        readonly Metadata signalingMetadata;
        readonly MediaOption mediaOption;
        readonly Role role;
        readonly CoreDispatcher dispatcher;

        IMediaStreamTrack localAudioTrack = null;
        IMediaStreamTrack localVideoTrack = null;
        readonly RemoteMediaTracksHolder remoteTracksHolder =
            new RemoteMediaTracksHolder();
        readonly StreamIdChecker streamIdChecker = new StreamIdChecker();

        public int TimeoutMs { get; set; } = 15000; // 15 seconds

        public MediaChannel(
            string signalingEndpoint,
            string channelId,
            Metadata signalingMetadata,
            MediaOption mediaOption)
            : this(signalingEndpoint, channelId, signalingMetadata,
                  mediaOption, CoreApplication.MainView.CoreWindow.Dispatcher)
        {
        }

        public MediaChannel(
            string         signalingEndpoint,
            string         channelId,
            Metadata       signalingMetadata,
            MediaOption    mediaOption,
            CoreDispatcher dispatcher)
        {
            this.signalingEndpoint = signalingEndpoint;
            this.channelId         = channelId;
            this.signalingMetadata = signalingMetadata;
            this.mediaOption       = mediaOption;
            this.role              = mediaOption.GetRequiredRole();
            this.dispatcher        = dispatcher;
        }

        void RunOnUiThread(Action fn)
        {
            var _async = dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, new DispatchedHandler(fn));
        }

        public void Connect()
        {
            Logger.Debug("MediaChannel", "Connect");

            if (closed)
            {
                Logger.Debug("MediaChannel", "already closed");
                return;
            }

            Logger.StartMediaTrace();

            mediaOption.DumpLog();
            StartPeerWithClientOffer();
            StartNegotiationTimer();
        }

        public void Disconnect()
        {
            Logger.Debug("MediaChannel", "Disconnect");

            if (closed)
            {
                Logger.Debug("MediaChannel", "already closed");
                return;
            }

            closed = true;

            StopNegotiationTimer();

            DisconnectSignalingChannel();

            Logger.Debug("MediaChannel", "try to close peer");


            CloseTempPeer();
            ClosePeer();

            Logger.StopMediaTrace();

            Logger.Debug("MediaChannel", "peer closed");

            RunOnUiThread(() => {
                OnDisconnect?.Invoke();
            });

        }

        void StartPeerWithClientOffer()
        {
            Logger.Debug("MediaChannel", "StartPeerWithClientOffer");

            Task.Run(async () => {

                var dummyConf = new OfferConfig();

                var rtcConf = NetworkConfiguration.Create(dummyConf, mediaOption);

                var tempMediaOption = new MediaOption() {
                    SpeakerDevice = mediaOption.SpeakerDevice,
                    VideoDownstreamEnabled = true,
                };

                tempPeer = new PeerChannel(rtcConf, tempMediaOption, dispatcher);

                tempPeer.OnError += TempPeer_OnError;
                tempPeer.OnClose += TempPeer_OnClose;

                if (!tempPeer.Initialize())
                {
                    Logger.Debug("MediaChannel", "failed to initialize peer");
                    RunOnUiThread(() => {
                        OnError?.Invoke(ErrorType.PeerCantBuild);
                    });
                    Disconnect();
                }

                var sdp = await tempPeer.GenerateClientOffer();

                CloseTempPeer();

                if (string.IsNullOrEmpty(sdp))
                {
                    Logger.Debug("MediaChannel", "failed to create capabilities");

                    RunOnUiThread(() => {
                        OnError?.Invoke(ErrorType.PeerSdpFailure);
                    });

                    Disconnect();
                }
                else
                {
                    ConnectSignalingChannel(sdp);
                }
            });

        }

        void CloseTempPeer()
        {
            if (tempPeer == null)
            {
                return;
            }

            tempPeer.OnError -= TempPeer_OnError;
            tempPeer.OnClose -= TempPeer_OnClose;

            tempPeer.Close();

            tempPeer = null;
        }

        void ConnectSignalingChannel(string sdp)
        {
            Logger.Debug("MediaChannel", "connect:signaling");

            if (signaling != null)
            {
                Logger.Debug("MediaChannel", "signaling channel is already connected");
                return;
            }

            signaling = new SignalingChannel(signalingEndpoint,
                role, channelId, mediaOption, signalingMetadata, sdp);

            signaling.OnConnect      += Signaling_OnConnect;
            signaling.OnDisconnect   += Signaling_OnDisconnect;
            signaling.OnError        += Signaling_OnError;
            signaling.OnInitialOffer += Signaling_OnInitialOffer;
            signaling.OnUpdatedOffer += Signaling_OnUpdatedOffer;
            signaling.OnReOffer      += Signaling_OnReOffer;

            signaling.Connect();
        }

        void DisconnectSignalingChannel()
        {
            Logger.Debug("MediaChannel", "disconnect:signaling");

            if (signaling == null)
            {
                Logger.Debug("MediaChannel", "signaling channel is not found");
                return;
            }

            signaling.OnConnect      -= Signaling_OnConnect;
            signaling.OnDisconnect   -= Signaling_OnDisconnect;
            signaling.OnError        -= Signaling_OnError;
            signaling.OnInitialOffer -= Signaling_OnInitialOffer;
            signaling.OnUpdatedOffer -= Signaling_OnUpdatedOffer;
            signaling.OnReOffer      -= Signaling_OnReOffer;

            signaling.Disconnect();

            signaling = null;
        }

        async void HandleInitialOffer(string sdp, OfferConfig offerConf)
        {
            Logger.Debug("MediaChannel", "Handle Initial Offer");

            if (peer != null)
            {
                Logger.Debug("MediaChannel", "peer not found");
                return;
            }

            var rtcConf = NetworkConfiguration.Create(offerConf, mediaOption);

            peer = new PeerChannel(rtcConf, mediaOption, dispatcher);

            peer.OnConnect            += Peer_OnConnect;
            peer.OnClose              += Peer_OnClose;
            peer.OnError              += Peer_OnError;
            peer.OnIceCandidate       += Peer_OnIceCandidate;
            peer.OnAddLocalAudioTrack += Peer_OnAddLocalAudioTrack;
            peer.OnAddLocalVideoTrack += Peer_OnAddLocalVideoTrack;
            peer.OnAddRemoteTrack     += Peer_OnAddRemoteTrack;
            peer.OnRemoveRemoteTrack  += Peer_OnRemoveRemoteTrack;

            streamIdChecker.PickMsidFromSdp(sdp);

            if (!peer.Initialize())
            {
                Logger.Debug("MediaChannel", "failed to initialize peer");
                RunOnUiThread(() => {
                    OnError?.Invoke(ErrorType.PeerCantBuild);
                });
                Disconnect();
            }

            var answer = await peer.HandleInitialRemoteOffer(sdp);

            if (string.IsNullOrEmpty(answer))
            {
                Logger.Debug("MediaChannel", "failed to create answer for remote offer");

                RunOnUiThread(() => {
                    OnError?.Invoke(ErrorType.PeerSdpFailure);
                });

                Disconnect();

                return;
            }

            await signaling.SendAnswerMessage(answer);
        }

        void ClosePeer()
        {
            Logger.Debug("MediaChannel", "Close Peer");

            if (peer == null)
            {
                Logger.Debug("MediaChannel", "peer not found");
                return;
            }

            peer.OnConnect            -= Peer_OnConnect;
            peer.OnClose              -= Peer_OnClose;
            peer.OnError              -= Peer_OnError;
            peer.OnIceCandidate       -= Peer_OnIceCandidate;
            peer.OnAddLocalAudioTrack -= Peer_OnAddLocalAudioTrack;
            peer.OnAddLocalVideoTrack -= Peer_OnAddLocalVideoTrack;
            peer.OnAddRemoteTrack     -= Peer_OnAddRemoteTrack;
            peer.OnRemoveRemoteTrack  -= Peer_OnRemoveRemoteTrack;

            Logger.Debug("MediaChannel", "dispose local media tracks");

            if (localAudioTrack != null)
            {
                Logger.Debug("MediaChannel", "dispose local audio track");
                (localAudioTrack as IDisposable)?.Dispose();
                localAudioTrack = null;
            }

            if (localVideoTrack != null)
            {
                Logger.Debug("MediaChannel", "dispose local video track");
                (localVideoTrack as IDisposable)?.Dispose();
                localVideoTrack = null;
            }

            Logger.Debug("MediaChannel", "dispose remote media tracks");
            remoteTracksHolder.Dispose();

            Logger.Debug("MediaChannel", "Close Peer");
            peer.Close();

            peer = null;
        }

        async void HandleUpdatedOffer(string sdp)
        {
            Logger.Debug("MediaChannel", "HandleUpdatedOffer");

            if (peer == null)
            {
                Logger.Debug("MediaChannel", "peer not found");
                return;
            }

            streamIdChecker.PickMsidFromSdp(sdp);

            var answer = await peer.HandleUpdatedRemoteOffer(sdp);

            if (string.IsNullOrEmpty(answer))
            {
                Logger.Debug("MediaChannel", "failed to create answer for remote offer");
                return;
            }

            await signaling.SendUpdateAnswerMessage(answer);
        }

        async void HandleReOffer(string sdp)
        {
            Logger.Debug("MediaChannel", "HandleReOffer");
            if (peer == null)
            {
                Logger.Debug("MediaChannel", "peer not found");
                return;
            }

            streamIdChecker.PickMsidFromSdp(sdp);

            var answer = await peer.HandleUpdatedRemoteOffer(sdp);

            if (string.IsNullOrEmpty(answer))
            {
                Logger.Debug("MediaChannel", "failed to create answer for remote offer");
                return;
            }

            await signaling.SendReAnswerMessage(answer);
        }

        #region Signaling Events

        void Signaling_OnConnect()
        {
            Logger.Debug("MediaChannel", "@signaling:connect");
        }

        void Signaling_OnDisconnect()
        {
            Logger.Debug("MediaChannel", "@signaling:disconnect");
            Task.Run(() => {
                Disconnect();
            });
        }

        void Signaling_OnInitialOffer(string clientId, string sdp, OfferConfig config)
        {
            Logger.Debug("MediaChannel", "@signaling:initial_offer");
            Task.Run(() => {
                this.clientId = clientId;
                HandleInitialOffer(sdp, config);
            });
        }

        void Signaling_OnReOffer(string sdp)
        {
            Logger.Debug("MediaChannel", "@signaling:re_offer");
            Task.Run(() => {
                HandleReOffer(sdp);
            });
        }

        void Signaling_OnUpdatedOffer(string sdp)
        {
            Logger.Debug("MediaChannel", "@signaling:updated_offer");
            Task.Run(() => {
                HandleUpdatedOffer(sdp);
            });
        }

        void Signaling_OnError(ErrorType error)
        {
            Logger.Debug("MediaChannel", "@signaling:error");
            Task.Run(() => {
                RunOnUiThread(() => {
                    OnError?.Invoke(error);
                });
                Disconnect();
            });
        }

        #endregion

        #region Peer Events

        void Peer_OnConnect()
        {
            Logger.Debug("MediaChannel", "@peer:connect");

            StopNegotiationTimer();

            RunOnUiThread(() => {
                OnConnect?.Invoke();
            });
        }

        void Peer_OnClose()
        {
            Logger.Debug("MediaChannel", "@peer:close");

            Disconnect();
        }

        void Peer_OnError(ErrorType error)
        {
            Logger.Debug("MediaChannel", "@peer:error");

            RunOnUiThread(() => {
                OnError?.Invoke(error);
            });

            Disconnect();
        }

        void Peer_OnIceCandidate(string sdp)
        {
            Logger.Debug("MediaChannel", "@peer:candidate");

            if (signaling != null)
            {
                var _async = signaling.SendCandidateMessage(sdp);
            }
        }

        void Peer_OnAddLocalAudioTrack(IMediaStreamTrack track)
        {
            Logger.Debug("MediaChannel", "@peer:local_audio_track:add");

            if (localAudioTrack == null)
            {
                localAudioTrack = track;
            }

            RunOnUiThread(() => {
                OnAddLocalAudioTrack?.Invoke(track);
            });
        }

        void Peer_OnAddLocalVideoTrack(IVideoCapturer capturer, IMediaStreamTrack track)
        {
            Logger.Debug("MediaChannel", "@peer:local_video_track:add");

            if (localVideoTrack == null)
            {
                localVideoTrack = track;
            }

            RunOnUiThread(() => {
                OnAddLocalVideoTrack?.Invoke(capturer, track);
            });
        }

        void Peer_OnAddRemoteTrack(IMediaStreamTrack track)
        {
            Logger.Debug("MediaChannel", "@peer:remote_track:add");

            RunOnUiThread(() => {

                if (remoteTracksHolder.AddTrack(track))
                {
                    var streamId = streamIdChecker.GetStreamIdForTrackId(track.Id);

                    Logger.Debug("MediaChannel", $"streamId:{streamId}");

                    if ( mediaOption.UpstreamIsRequired()
                      && clientId == streamId)
                    {
                        Logger.Debug("MediaChannel", "ignore, this track is mine");
                        return;
                    }

                    if (track.Kind == "audio")
                    {
                        OnAddRemoteAudioTrack?.Invoke(track);
                    }
                    else if (track.Kind == "video")
                    {
                        OnAddRemoteVideoTrack?.Invoke(track);
                    }
                }

            });
        }

        void Peer_OnRemoveRemoteTrack(string kind, string trackId)
        {
            Logger.Debug("MediaChannel", "@peer:remote_track:remove");

            RunOnUiThread(() => {

                if (clientId == streamIdChecker.GetStreamIdForTrackId(trackId))
                {
                    Logger.Debug("MediaChannel", "ignore, this track is mine");
                    return;
                }

                if (kind == "audio")
                {
                    OnRemoveRemoteAudioTrack?.Invoke(trackId);
                }
                else if (kind == "video")
                {
                    OnRemoveRemoteVideoTrack?.Invoke(trackId);
                }

                // 該当trackをDisposeしてしまうので、
                // OnRemoveRemoteTrackイベントの後に呼ぶこと
                remoteTracksHolder.RemoveTrack(kind, trackId);

            });
        }

        #endregion

        #region TempPeer Events

        void TempPeer_OnClose()
        {
            Logger.Debug("MediaChannel", "temp_peer:closed");
        }

        void TempPeer_OnError(ErrorType error)
        {
            Logger.Debug("MediaChannel", "temp_peer:error");
            Disconnect();
        }

        #endregion

        #region Timer Events

        void StartNegotiationTimer()
        {
            Logger.Debug("MediaChannel", "StartNegotiationTimer");

            timer = new Timer(Timer_OnElapsed,
                null, TimeoutMs, Timeout.Infinite);
        }

        void StopNegotiationTimer()
        {
            Logger.Debug("MediaChannel", "StopNegotiationTimer");

            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        void OnNegotiationTimeout()
        {
            Logger.Debug("MediaChannel", "OnNegotiationTimeout");

            RunOnUiThread(() => {
                OnError?.Invoke(ErrorType.NegotiationTimeout);
            });

            Disconnect();
        }

        void Timer_OnElapsed(object status)
        {
            Logger.Debug("MediaChannel", "OnTimerElapsed");

            StopNegotiationTimer();
            OnNegotiationTimeout();
        }

        #endregion

    }

}
