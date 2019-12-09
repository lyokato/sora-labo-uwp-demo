using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.ComponentModel;
using Windows.UI.Core;

using Org.WebRtc;

namespace SoraDemo.ViewModels
{
    public class MediaChannelViewModel : INotifyPropertyChanged
    {
        readonly CoreDispatcher dispatcher;

        IMediaStreamTrack localAudioTrack;
        IMediaStreamTrack localVideoTrack;

        // TODO multiple track support
        IMediaStreamTrack remoteAudioTrack;
        IMediaStreamTrack remoteVideoTrack;

        public MediaChannelViewModel(CoreDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        void RunOnUiThread(Action fn)
        {
            var async = dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, 
                new DispatchedHandler(fn));
        }

        Sora.MediaOption mediaOption = new Sora.MediaOption();

        public Windows.UI.Xaml.Controls.MediaElement LocalVideoView { get; set; }
        public Windows.UI.Xaml.Controls.MediaElement RemoteVideoView { get; set; }

        #region Properties for DataBinding

        public event PropertyChangedEventHandler PropertyChanged;

        string _channelId = "demo";
        
        public string ChannelId
        {
            get { return _channelId;  }
            set
            {
                _channelId = value;
                OnPropertyChanged(nameof(ChannelId));
            }
        }

        public List<Sora.VideoOption.Codec> AvailableVideoCodecs
        {
            get
            {
                var keys = Enum.GetValues(typeof(Sora.VideoOption.Codec)).Cast<Sora.VideoOption.Codec>();
                return new List<Sora.VideoOption.Codec>(keys.ToList());
            }
        }

        public List<Sora.AudioOption.Codec> AvailableAudioCodecs
        {
            get
            {
                var keys = Enum.GetValues(typeof(Sora.AudioOption.Codec)).Cast<Sora.AudioOption.Codec>();
                return new List<Sora.AudioOption.Codec>(keys.ToList());
            }
        }

        public Sora.VideoOption.Codec SelectedVideoCodec
        {
            get { return mediaOption.VideoCodec; }
            set
            {
                mediaOption.VideoCodec = value;
                OnPropertyChanged(nameof(SelectedVideoCodec));
            }
        }

        public Sora.AudioOption.Codec SelectedAudioCodec
        {
            get { return mediaOption.AudioCodec; }
            set
            {
                mediaOption.AudioCodec = value;
                OnPropertyChanged(nameof(SelectedAudioCodec));
            }
        }

        public bool VideoDownstreamEnabled
        {
            get { return mediaOption.VideoDownstreamEnabled; }
            set
            {
                mediaOption.VideoDownstreamEnabled = value;
                OnPropertyChanged(nameof(VideoDownstreamEnabled));
            }
        }

        private bool _audioDownstreamEnabled = true;
        public bool AudioDownstreamEnabled
        {
            get { return _audioDownstreamEnabled; }
            set
            {
                _audioDownstreamEnabled = value;
                OnPropertyChanged(nameof(AudioDownstreamEnabled));
            }
        }

        private bool _audioUpstreamEnabled = true;
        public bool AudioUpstreamEnabled
        {
            get { return _audioUpstreamEnabled; }
            set
            {
                _audioUpstreamEnabled = value;
                OnPropertyChanged(nameof(AudioUpstreamEnabled));
            }
        }

        public bool MultistreamEnabled
        {
            get { return mediaOption.MultistreamEnabled; }
            set
            {
                mediaOption.MultistreamEnabled = value;
                OnPropertyChanged(nameof(MultistreamEnabled));
            }
        }

        private bool _videoUpstreamEnabled = true;
        public bool VideoUpstreamEnabled
        {
            get { return _videoUpstreamEnabled; }
            set
            {
                _videoUpstreamEnabled = value;
                OnPropertyChanged(nameof(VideoUpstreamEnabled));
            }
        }

        private ObservableCollection<Sora.Device.MediaDevice> _speakerDevices;
        public ObservableCollection<Sora.Device.MediaDevice> SpeakerDevices
        {
            get { return _speakerDevices; }
            set
            {
                if (_speakerDevices == value)
                {
                    return;
                }
                _speakerDevices = value;
                OnPropertyChanged(nameof(SpeakerDevices));
            }
        }

        private Sora.Device.MediaDevice _selectedSpeakerDevice;
        public Sora.Device.MediaDevice SelectedSpeakerDevice
        {
            get { return _selectedSpeakerDevice; }
            set
            {
                _selectedSpeakerDevice = value;
                OnPropertyChanged(nameof(SelectedSpeakerDevice));
            }
        }

        private ObservableCollection<Sora.Device.MediaDevice> _microphoneDevices;
        public ObservableCollection<Sora.Device.MediaDevice> MicrophoneDevices
        {
            get { return _microphoneDevices; }
            set
            {
                if (_microphoneDevices == value)
                {
                    return;
                }
                _microphoneDevices = value;
                OnPropertyChanged(nameof(MicrophoneDevices));
            }
        }

        private Sora.Device.MediaDevice _selectedMicrophoneDevice;
        public Sora.Device.MediaDevice SelectedMicrophoneDevice
        {
            get { return _selectedMicrophoneDevice; }
            set
            {
                _selectedMicrophoneDevice = value;
                OnPropertyChanged(nameof(SelectedMicrophoneDevice));
            }
        }

        private ObservableCollection<Sora.Device.CaptureCapability> _videoCaptureCapabilities;
        public ObservableCollection<Sora.Device.CaptureCapability> VideoCaptureCapabilities
        {
            get { return _videoCaptureCapabilities; }
            set
            {
                if (_videoCaptureCapabilities == value)
                {
                    return;
                }
                _videoCaptureCapabilities = value;
                OnPropertyChanged(nameof(VideoCaptureCapabilities));
            }
        }

        private Sora.Device.CaptureCapability _selectedVideoCaptureCapability;
        public Sora.Device.CaptureCapability SelectedVideoCaptureCapability
        {
            get { return _selectedVideoCaptureCapability; }
            set
            {
                _selectedVideoCaptureCapability = value;
                OnPropertyChanged(nameof(SelectedVideoCaptureCapability));
            }
        }

        private string _stateText = "OFFLINE";

        public string StateText
        {
            get { return _stateText; }
            set
            {
                _stateText = value;
                OnPropertyChanged(nameof(StateText));
            }
        }

        bool _isRunning = false;
        public bool IsRunning
        {
            get { return _isRunning;  }
            set
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
            }
        }
        bool _isNotRunning = true;
        public bool IsNotRunning
        {
            get { return _isNotRunning;  }
            set
            {
                _isNotRunning = value;
                OnPropertyChanged(nameof(IsNotRunning));
            }
        }


        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, 
                new PropertyChangedEventArgs(name));
        }

        #endregion

        async Task RefreshMediaDevices()
        {
            // CaptureDeviceの取得は必ずUIThreadでやること
            var videoCapabilities = 
                await Sora.Device.MediaDevice.GetAllVideoCapturerCapabilities();

            VideoCaptureCapabilities = 
                new ObservableCollection<Sora.Device.CaptureCapability>(videoCapabilities);

            if (VideoCaptureCapabilities.Count > 0)
            {
                if (SelectedVideoCaptureCapability == null || 
                    !VideoCaptureCapabilities.Contains(SelectedVideoCaptureCapability))
                {
                    SelectedVideoCaptureCapability = VideoCaptureCapabilities.First();
                }
            }
            else
            {
                SelectedVideoCaptureCapability = null;
            }

            var microphones =
                await Sora.Device.MediaDevice.GetMicrophoneDevices();

            MicrophoneDevices = 
                new ObservableCollection<Sora.Device.MediaDevice>(microphones);

            if (MicrophoneDevices.Count > 0)
            {
                if (SelectedMicrophoneDevice == null || 
                    !MicrophoneDevices.Contains(SelectedMicrophoneDevice))
                {
                    SelectedMicrophoneDevice = MicrophoneDevices.First();
                }
            }
            else
            {
                SelectedMicrophoneDevice = null;
            }

            var speakers =
                await Sora.Device.MediaDevice.GetSpeakerDevices();

            SpeakerDevices = 
                new ObservableCollection<Sora.Device.MediaDevice>(speakers);

            if (SpeakerDevices.Count > 0)
            {
                if (SelectedSpeakerDevice == null || 
                    !SpeakerDevices.Contains(SelectedSpeakerDevice))
                {
                    SelectedSpeakerDevice = SpeakerDevices.First();
                }
            }
            else
            {
                SelectedSpeakerDevice = null;
            }

        }

        public async void Initialize()
        {
            await Sora.Initializer.Init(dispatcher, true, true, true);

            RunOnUiThread(async () => {
               await RefreshMediaDevices();
            });
        }

        public void DumpOptions()
        {
            mediaOption.DumpLog();
        }

        Sora.MediaChannel mediaChannel = null;

        public void Start()
        {
            if (mediaChannel != null)
            {
                Debug.WriteLine("media channel is already running");
                return;
            }

            var metadata =
                new Sora.Signaling.Metadata()
                {
                    SignalingKey = Config.SoraLaboSignalingKey
                };

            var channelId = $"{Config.SoraLaboUsername}@{ChannelId}";

            mediaOption.SpeakerDevice = _audioDownstreamEnabled ? _selectedSpeakerDevice : null;
            mediaOption.MicrophoneDevice = _audioUpstreamEnabled ? _selectedMicrophoneDevice : null;
            mediaOption.VideoCaptureCapability = _videoUpstreamEnabled ? _selectedVideoCaptureCapability : null;

            mediaChannel = 
                new Sora.MediaChannel(Config.SoraLaboEndpoint, channelId, 
                    metadata, mediaOption);

            mediaChannel.OnConnect                += Channel_OnConnect;
            mediaChannel.OnDisconnect             += Channel_OnDisconnect;
            mediaChannel.OnError                  += Channel_OnError;
            mediaChannel.OnAddRemoteAudioTrack    += Channel_OnAddRemoteAudioTrack;
            mediaChannel.OnRemoveRemoteAudioTrack += Channel_OnRemoveRemoteAudioTrack;
            mediaChannel.OnAddRemoteVideoTrack    += Channel_OnAddRemoteVideoTrack;
            mediaChannel.OnRemoveRemoteVideoTrack += Channel_OnRemoveRemoteVideoTrack;
            mediaChannel.OnAddLocalVideoTrack     += Channel_OnAddLocalVideoTrack;
            mediaChannel.OnAddLocalAudioTrack     += Channel_OnAddLocalAudioTrack;

            mediaChannel.Connect();

            IsRunning = true;
            IsNotRunning = false;
            StateText = "CONNECTING";
        }

        public void Stop()
        {
            if (mediaChannel == null)
            {
                Debug.WriteLine("media channel is already stopped");
                return;
            }

            mediaChannel.OnConnect                -= Channel_OnConnect;
            mediaChannel.OnDisconnect             -= Channel_OnDisconnect;
            mediaChannel.OnError                  -= Channel_OnError;
            mediaChannel.OnAddRemoteAudioTrack    -= Channel_OnAddRemoteAudioTrack;
            mediaChannel.OnRemoveRemoteAudioTrack -= Channel_OnRemoveRemoteAudioTrack;
            mediaChannel.OnAddRemoteVideoTrack    -= Channel_OnAddRemoteVideoTrack;
            mediaChannel.OnRemoveRemoteVideoTrack -= Channel_OnRemoveRemoteVideoTrack;
            mediaChannel.OnAddLocalVideoTrack     -= Channel_OnAddLocalVideoTrack;
            mediaChannel.OnAddLocalAudioTrack     -= Channel_OnAddLocalAudioTrack;

            localAudioTrack = null;
            remoteAudioTrack = null;

            if (localVideoTrack != null)
            {
                localVideoTrack.Element = null;
                localVideoTrack = null;
            }

            if (remoteVideoTrack != null)
            {
                remoteVideoTrack.Element = null;
                remoteVideoTrack = null;
            }


            mediaChannel.Disconnect();
            mediaChannel = null;

            StateText = "OFFLINE";
            IsRunning = false;
            IsNotRunning = true;
        }

        #region Channel Events

        void Channel_OnAddLocalVideoTrack(
            IVideoCapturer capturer, IMediaStreamTrack track)
        {
            Sora.Logger.Debug("ViewModel", "Channel_OnAddLocalVideo");

            if (localVideoTrack != null)
            {
                Debug.WriteLine("local video track already exists");
                return;
            }

            localVideoTrack = track;
            localVideoTrack.Enabled = true;
            localVideoTrack.Element = 
                 MediaElementMaker.Bind(LocalVideoView);
        }

        void Channel_OnAddLocalAudioTrack(IMediaStreamTrack track)
        {
            Sora.Logger.Debug("ViewModel", "Channel_OnAddLocalAudio");

            if (localAudioTrack != null)
            {
                Sora.Logger.Debug("ViewModel", "local audio already exists");
                return;
            }

            localAudioTrack = track;
            localAudioTrack.Enabled = true;
        }

        void Channel_OnAddRemoteAudioTrack(IMediaStreamTrack track)
        {
            Sora.Logger.Debug("ViewModel", "Channel_OnAddRemoteAudio");

            if (remoteAudioTrack != null)
            {
                Sora.Logger.Debug("ViewModel", "remote audio already exists");
                return;
            }

            remoteAudioTrack = track;
            remoteAudioTrack.Enabled = true;
        }

        void Channel_OnAddRemoteVideoTrack(IMediaStreamTrack track)
        {
            Sora.Logger.Debug("ViewModel", "Channel_OnAddRemoteVideo");

            // TODO check trackId
            if (remoteVideoTrack == null)
            {
                Sora.Logger.Debug("ViewModel", "try to bind video to view");
                remoteVideoTrack = track;
                remoteVideoTrack.Element = 
                    MediaElementMaker.Bind(RemoteVideoView);
                Sora.Logger.Debug("ViewModel", "bound video to view");
            }

        }

        void Channel_OnRemoveRemoteVideoTrack(string trackId) 
        {
            Sora.Logger.Debug("ViewModel", "Channel_OnRemoveRemoteVideo");

            if (remoteVideoTrack != null)
            {
                Sora.Logger.Debug("ViewModel", "try to unbound video from view");
                remoteVideoTrack.Element = null;
                remoteVideoTrack = null;
                Sora.Logger.Debug("ViewModel", "unbound video from view");
            }
        }

        void Channel_OnRemoveRemoteAudioTrack(string trackId) 
        {
            Sora.Logger.Debug("ViewModel", "Channel_OnRemoveRemoteAudio");
        }

        void Channel_OnConnect()
        {
            Sora.Logger.Debug("ViewModel", "Channel_OnConnect");

            StateText = "ONLINE";
        }

        void Channel_OnError(Sora.ErrorType error)
        {
            Sora.Logger.Debug("ViewModel", "Channel_OnError");
            Debug.WriteLine($"Channel_OnError: {error}");
        }

        void Channel_OnDisconnect()
        {
            Sora.Logger.Debug("ViewModel", "Channel_OnDisconnect");
            Debug.WriteLine("Channel_OnDisconnect");

            Stop();
        }

        #endregion
    }

}
