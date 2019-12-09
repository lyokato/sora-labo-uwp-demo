using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using Windows.UI.Popups;

using Windows.ApplicationModel.Core;

namespace SoraDemo.Views
{
    public sealed partial class MainPage : Page
    {
        ViewModels.MediaChannelViewModel mediaChannel;

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(800, 600);
            ApplicationView.PreferredLaunchWindowingMode = 
                ApplicationViewWindowingMode.PreferredLaunchViewSize;

            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            mediaChannel = new ViewModels.MediaChannelViewModel(dispatcher);

            this.DataContext = mediaChannel;

            mediaChannel.LocalVideoView = localVideoView;
            mediaChannel.RemoteVideoView = remoteVideoView;

            // XXX MainPage表示前にやらせるべきかも
            mediaChannel.Initialize();
        }

        
        async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(mediaChannel.ChannelId))
            {
                var dlg = new MessageDialog("'Channel Name' is empty.");
                await dlg.ShowAsync();
                return;
            }

            if (   mediaChannel.AudioDownstreamEnabled
                && mediaChannel.SelectedSpeakerDevice == null)
            {
                var dlg = new MessageDialog("You should choose 'Speaker Device' if you want to enable audio downsteram.");
                await dlg.ShowAsync();
                return;
            }

            if (   mediaChannel.AudioUpstreamEnabled
                && mediaChannel.SelectedMicrophoneDevice == null)
            {
                var dlg = new MessageDialog("You should choose 'Microphone Device' if you want to enable audio upsteram.");
                await dlg.ShowAsync();
                return;
            }

            if (   mediaChannel.VideoUpstreamEnabled 
                && mediaChannel.SelectedVideoCaptureCapability == null)
            {
                var dlg = new MessageDialog("You should choose 'Video Capture Capability' if you want to enable video upsteram.");
                await dlg.ShowAsync();
                return;
            }

            mediaChannel.DumpOptions();
            mediaChannel.Start();
        }

        void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            mediaChannel.Stop();
        }

        void RemoteVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Debug.WriteLine("RemoteVideo MediaFailed");
        }

        void LocalVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Debug.WriteLine("LocalVideo MediaFailed");
        }

    }
}
