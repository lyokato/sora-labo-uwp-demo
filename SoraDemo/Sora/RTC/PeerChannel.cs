using System;
using System.Threading.Tasks;
using Windows.UI.Core;

using Org.WebRtc;

namespace Sora.RTC
{
    internal class PeerChannel
    {
        public Action OnConnect;
        public Action OnClose;
        public Action<ErrorType> OnError;
        public Action<string> OnIceCandidate;
        public Action<IMediaStreamTrack> OnAddLocalAudioTrack;
        public Action<IVideoCapturer, IMediaStreamTrack> OnAddLocalVideoTrack;
        public Action<IMediaStreamTrack> OnAddRemoteTrack;
        public Action<string, string> OnRemoveRemoteTrack;

        bool closed = false;
        readonly CoreDispatcher dispatcher;
        WebRtcFactory factory;

        readonly object connLock = new object();
        RTCPeerConnection rawConn;

        RTCPeerConnection Conn
        {
            get
            {
                lock (connLock)
                {
                    return rawConn;
                }
            }

            set
            {
                lock (connLock)
                {
                    if (null == value)
                    {
                        if (null != rawConn)
                        {
                            Logger.Debug("PeerChannel", "dispose peer connection");
                            (rawConn as IDisposable).Dispose();
                        }
                    }
                    rawConn = value;
                }
            }
        }


        Device.Screen screen = null;

        readonly RTCConfiguration rtcConf;
        readonly MediaOption mediaOption;

        public PeerChannel(RTCConfiguration rtcConf, 
            MediaOption mediaOption, CoreDispatcher dispatcher)
        {
            this.rtcConf     = rtcConf;
            this.mediaOption = mediaOption;
            this.dispatcher  = dispatcher;
        }

        public bool Initialize()
        {
            if (!CreatePeer())
            {
                Logger.Debug("PeerChannel", "failed to create peer");
                return false;
            }
            return true;
        }

        public async Task<string> GenerateClientOffer()
        {
            Logger.Debug("PeerChannel", "GenerateClientOffer");

            if (Conn == null)
            {
                Logger.Debug("PeerChannel", "should initialize beforehand");
                return string.Empty;
            }

            Conn.AddTransceiver("audio");

            Conn.AddTransceiver("video");

            var opts = new RTCOfferOptions();

            var capabilities = await Conn.CreateOffer(opts);

            return capabilities.Sdp;
        }

        public async Task<string> HandleInitialRemoteOffer(string sdp)
        {
            Logger.Debug("PeerChannel", "HandleInitialRemoteOffer");

            if (Conn == null)
            {
                Logger.Debug("PeerChannel", "should initialize beforehand");
                return string.Empty;
            }

            var sdpInit = new RTCSessionDescriptionInit()
            {
                Type = RTCSdpType.Offer,
                Sdp  = sdp,
            };

            Logger.Debug("PeerChannel", "SetRemoteOffer - SetRemoteDescription");

            await Conn.SetRemoteDescription(new RTCSessionDescription(sdpInit));

            if (mediaOption.VideoUpstreamEnabled)
            {
                Logger.Debug("PeerChannel", "video upstream setting");

                if (screen != null)
                {
                    await screen.StartCaptureAsync();
                }

                SetupVideoTrack();
            }

            if (mediaOption.AudioUpstreamEnabled)
            {
                Logger.Debug("PeerChannel", "audio upstream setting");

                SetupAudioTrack();
            }

            var opts = new RTCAnswerOptions();

            Logger.Debug("PeerChannel", "SetRemoteOffer - CreateAnswer");

            var answer = await Conn.CreateAnswer(opts);

            Logger.Debug("PeerChannel", answer.Sdp);

            await Conn.SetLocalDescription(answer);

            Logger.Debug("PeerChannel", "local description set");

            return answer.Sdp;
        }

        public async Task<string> HandleUpdatedRemoteOffer(string sdp)
        {
            Logger.Debug("PeerChannel", "HandleUpdatedRemoteOffer");

            var sdpInit = new RTCSessionDescriptionInit()
            {
                Type = RTCSdpType.Offer,
                Sdp  = sdp,
            };

            Logger.Debug("PeerChannel", "SetRemoteOffer - SetRemoteDescription");

            await Conn.SetRemoteDescription(new RTCSessionDescription(sdpInit));

            var opts = new RTCAnswerOptions();

            Logger.Debug("PeerChannel", "SetRemoteOffer - CreateAnswer");

            var answer = await Conn.CreateAnswer(opts);

            Logger.Debug("PeerChannel", answer.Sdp);

            await Conn.SetLocalDescription(answer);

            Logger.Debug("PeerChannel", "local description set");

            return answer.Sdp;
        }

        public void Close()
        {
            Logger.Debug("PeerChannel", "Close");

            if (closed)
            {
                Logger.Debug("PeerChannel", "Close - already closed");
                return;
            }
            
            CloseInternal(false);
        }

        void CloseInternal(bool delay)
        {
            Logger.Debug("PeerChannel", "CloseInternal");

            if (closed)
            {
                Logger.Debug("PeerChannel", "Close - already closed");
                return;
            }

            closed = true;

            Task.Run(() => { 

                screen?.Dispose();

                ClosePeer();

                factory?.Dispose();

                OnClose?.Invoke();

                GC.Collect();

            });

        }

        void CreateFactory()
        {
            Logger.Debug("PeerChannel", "CreateFactory");

            if (factory == null)
            {
                Logger.Debug("PeerChannel", "factory not found, create it.");

                var factoryConf = new WebRtcFactoryConfiguration();
                // factoryConf.EnableAudioBufferEvents = true;

                if (mediaOption.AudioUpstreamEnabled)
                {
                    Logger.Debug("PeerChannel", $"audio upstream setting found, set device id: {mediaOption.MicrophoneDevice.Id}");
                    factoryConf.AudioCaptureDeviceId = mediaOption.MicrophoneDevice.Id;
                    //factoryConf.AudioRenderingEnabled = true;
                }
                else
                {
                    //factoryConf.AudioRenderingEnabled = false;
                }

                if (mediaOption.AudioDownstreamEnabled)
                {
                    Logger.Debug("PeerChannel", $"audio downstream setting found, set device id: {mediaOption.SpeakerDevice.Id}");
                    factoryConf.AudioRenderDeviceId = mediaOption.SpeakerDevice.Id;
                    //factoryConf.AudioCapturingEnabled = true;
                }
                else
                {
                    //factoryConf.AudioCapturingEnabled = false;
                }

                if (mediaOption.VideoUpstreamEnabled && mediaOption.VideoCaptureCapability.DeviceId.Equals(Device.Screen.DeviceId))
                {
                    Logger.Debug("PeerChannel", "screen-share setting found, set device id");
                    factoryConf.CustomVideoFactory = screen.CreateCapturerFactory();
                }
                else
                {
                    factoryConf.CustomVideoFactory = null;
                }

                factory = new WebRtcFactory(factoryConf);

                /*
                factory.OnAudioPostCaptureInitialize     += Factory_OnAudioPostCaptureInit;
                factory.OnAudioPostCaptureRuntimeSetting += Factory_OnAudioPostCaptureRuntimeSetting;
                factory.OnAudioPostCapture               += Factory_OnAudioPostCaptureBuffer;

                factory.OnAudioPreRenderInitialize     += Factory_OnAudioPreRenderInit;
                factory.OnAudioPreRenderRuntimeSetting += Factory_OnAudioPreRenderRuntimeSetting;
                factory.OnAudioPreRender               += Factory_OnAudioPreRenderBuffer;
                */
            }

        }

        bool CreatePeer()
        {
            Logger.Debug("PeerChannel", "CreatePeer");

            if (Conn != null)
            {
                Logger.Debug("PeerChannel", "CreatePeer - already exists");
                return false;
            }

            if (mediaOption.VideoUpstreamEnabled &&
                mediaOption.VideoCaptureCapability.DeviceId.Equals(Device.Screen.DeviceId))
            {
                Logger.Debug("PeerChannel", "screen-share setting found, prepare.");
                this.screen = new Device.Screen(dispatcher);
            }

            CreateFactory();

            rtcConf.Factory = factory;

            Logger.Debug("PeerChannel", "create PeerConnection");
            Conn = new RTCPeerConnection(rtcConf);
            if (Conn == null)
            {
                Logger.Debug("PeerChannel", "failed to create PeerConnection");
                OnError?.Invoke(ErrorType.PeerCantBuild);
                return false;
            }

            Conn.OnIceConnectionStateChange += Conn_OnIceConnectionStateChange;
            Conn.OnSignalingStateChange     += Conn_OnSignalingStateChange;
            Conn.OnIceCandidate             += Conn_OnIceCandidate;
            Conn.OnTrack                    += Conn_OnAddTrack;
            Conn.OnRemoveTrack              += Conn_OnRemoveTrack;
            Conn.OnConnectionStateChange    += Conn_OnConnectionStateChange;
            Conn.OnIceGatheringStateChange  += Conn_OnIceGatheringStateChange;
            Conn.OnNegotiationNeeded        += Conn_OnNegotiationNeeded;

            return true;
        }

        void SetupVideoTrack()
        {
            Logger.Debug("PeerChannel", "SetupVideoTrack");

            var capability = mediaOption.VideoCaptureCapability;

            Logger.Debug("PeerChannel", "create video capturer");

            var capturerParams = capability.CreationParameters(factory);
            var capturer = VideoCapturer.Create(capturerParams);

            if (capturer == null)
            {
                Logger.Debug("PeerChannel", 
                    "failed to create video capturer");
                return;
            }

            Logger.Debug("PeerChannel", "create video track");

            var track = MediaStreamTrack.CreateVideoTrack(factory,
                mediaOption.VideoTrackId, capturer);

            (capturer as IDisposable).Dispose();

            Logger.Debug("PeerChannel", "add video track");

            Conn.AddTrack(track);

            Logger.Debug("PeerChannel", "add track");

            OnAddLocalVideoTrack?.Invoke(capturer, track);
        }

        void SetupAudioTrack()
        {
            Logger.Debug("PeerChannel", "SetupAudioTrack");

            var opts = new AudioOptions
            {
                Factory = factory
            };

            Logger.Debug("PeerChannel", "create audio source");

            var audioSource = AudioTrackSource.Create(opts);

            Logger.Debug("PeerChannel", "create audio track");

            var track = MediaStreamTrack.CreateAudioTrack(factory,
                mediaOption.AudioTrackId, audioSource);

            Logger.Debug("PeerChannel", "add audio track");

            Conn.AddTrack(track);

            OnAddLocalAudioTrack?.Invoke(track);
        }

        void ClosePeer()
        {
            Logger.Debug("PeerChannel", "ClosePeer");

            if (Conn != null)
            {
                Conn.OnIceConnectionStateChange -= Conn_OnIceConnectionStateChange;
                Conn.OnSignalingStateChange     -= Conn_OnSignalingStateChange;
                Conn.OnIceCandidate             -= Conn_OnIceCandidate;
                Conn.OnTrack                    -= Conn_OnAddTrack;
                Conn.OnRemoveTrack              -= Conn_OnRemoveTrack;
                Conn.OnConnectionStateChange    -= Conn_OnConnectionStateChange;
                Conn.OnIceGatheringStateChange  -= Conn_OnIceGatheringStateChange;
                Conn.OnNegotiationNeeded        -= Conn_OnNegotiationNeeded;

                Conn = null;
            }

        }

        #region PeerConnection Events

        void Conn_OnConnectionStateChange()
        {
            Logger.Debug("PeerChannel", 
                $"OnConnectionStateChange");
        }

        void Conn_OnIceGatheringStateChange()
        {
            Logger.Debug("PeerChannel", 
                $"OnIceGatheringStateChange - {Conn.IceGatheringState}");
        }

        void Conn_OnNegotiationNeeded()
        {
            Logger.Debug("PeerChannel", 
                $"OnNegotiationNeeded");
        }

        void Conn_OnIceConnectionStateChange()
        {
            Logger.Debug("PeerChannel", 
                $"OnIceConnectionStateChange - {Conn.IceConnectionState}");

            switch (Conn.IceConnectionState)
            {
                case RTCIceConnectionState.Connected:
                    OnConnect?.Invoke();
                    break;
                case RTCIceConnectionState.Failed:
                    OnError?.Invoke(ErrorType.PeerIceFailed);
                    break;
                case RTCIceConnectionState.Disconnected:
                case RTCIceConnectionState.Closed:
                    CloseInternal(true);
                    break;
                default:
                    break;
            }
        }

        void Conn_OnSignalingStateChange()
        {
            Logger.Debug("PeerChannel", 
                $"OnSignalingStateChange - {Conn.SignalingState}");
        }

        void Conn_OnIceCandidate(IRTCPeerConnectionIceEvent evt)
        {
            Logger.Debug("PeerChannel", $"OnIceCandidate");

            OnIceCandidate?.Invoke(evt.Candidate.Candidate);
        }

        void Conn_OnAddTrack(IRTCTrackEvent evt)
        {
            Logger.Debug("PeerChannel", 
                $"OnAddTrack:{evt.Track.Kind}:{evt.Track.Id}:{evt.Track.GetHashCode()}");

            if (evt.Transceiver != null)
            {
                Logger.Debug("PeerChannel", "Transceiver found");

                if (evt.Transceiver.Sender != null)
                {
                    Logger.Debug("PeerChannel", "Sender found");
                }
                else
                {
                    Logger.Debug("PeerChannel", "Sender not found");
                }

                if (evt.Transceiver.Receiver != null)
                {
                    Logger.Debug("PeerChannel", "Receiver found");
                    var track = evt.Transceiver.Receiver.Track;
                    OnAddRemoteTrack?.Invoke(evt.Track);
                }
                else
                {
                    Logger.Debug("PeerChannel", "Receiver not found");
                }
            }
            else
            {
                Logger.Debug("PeerChannel", "Transceiver not found");
            }
        }

        void Conn_OnRemoveTrack(IRTCTrackEvent evt)
        {
            Logger.Debug("PeerChannel", 
                $"OnRemoveTrack:{evt.Track.Kind}:{evt.Track.Id}:{evt.Track.GetHashCode()}");


            if (evt.Transceiver != null)
            {
                Logger.Debug("PeerChannel", "Transceiver found");

                if (evt.Transceiver.Sender != null)
                {
                    Logger.Debug("PeerChannel", "Sender found");
                }
                else
                {
                    Logger.Debug("PeerChannel", "Sender not found");
                }

                if (evt.Transceiver.Receiver != null)
                {
                    Logger.Debug("PeerChannel", "Receiver found");
                    var track = evt.Transceiver.Receiver.Track;
                    OnRemoveRemoteTrack?.Invoke(track.Kind, track.Id);
                }
                else
                {
                    Logger.Debug("PeerChannel", "Receiver not found");
                }
            }
            else
            {
                Logger.Debug("PeerChannel", "Transceiver not found");
            }
        }

        #endregion

        #region Factory Events

        void Factory_OnAudioPostCaptureInit(IAudioProcessingInitializeEvent evt)
        {
            Logger.Debug("PeerChannel", "Factory_OnAudioPostCaptureInit");
            (evt as IDisposable).Dispose();
        }

        void Factory_OnAudioPostCaptureRuntimeSetting(IAudioProcessingRuntimeSettingEvent evt)
        {
            Logger.Debug("PeerChannel", "Factory_OnAudioPostCaptureRuntimeSetting");
            (evt as IDisposable).Dispose();
        }

        void Factory_OnAudioPostCaptureBuffer(IAudioBufferEvent evt)
        {
            Logger.Debug("PeerChannel", "Factory_OnAudioPostCaptureBuffer");
            (evt as IDisposable).Dispose();
        }

        void Factory_OnAudioPreRenderInit(IAudioProcessingInitializeEvent evt)
        {
            Logger.Debug("PeerChannel", "Factory_OnAudioPreRenderInit");
            (evt as IDisposable).Dispose();
        }

        void Factory_OnAudioPreRenderRuntimeSetting(IAudioProcessingRuntimeSettingEvent evt)
        {
            Logger.Debug("PeerChannel", "Factory_OnAudioPreRenderRuntimeSetting");
            (evt as IDisposable).Dispose();
        }

        void Factory_OnAudioPreRenderBuffer(IAudioBufferEvent evt)
        {
            Logger.Debug("PeerChannel", "Factory_OnAudioPreRenderBuffer");
            (evt as IDisposable).Dispose();
        }

        #endregion

    }
}
