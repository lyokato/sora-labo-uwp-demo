using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Sora.Signaling
{
    internal class SignalingChannel
    {
        public event Action OnConnect;
        public event Action OnDisconnect;
        public event Action<string, string, OfferConfig> OnInitialOffer;
        public event Action<string> OnUpdatedOffer;
        public event Action<string> OnReOffer;
        public event Action<ErrorType> OnError;

        MessageWebSocket socket;
        DataWriter socketWriter;

        readonly string      endpoint;
        readonly string      channelId;
        readonly Metadata    metadata;
        readonly string      capability;
        readonly Role        role;
        readonly MediaOption mediaOption;

        bool closed = false;

        public SignalingChannel(
            string      endpoint, 
            Role        role, 
            string      channelId, 
            MediaOption mediaOption, 
            Metadata    metadata, 
            string      capability) 
        {

            this.endpoint     = endpoint;
            this.channelId    = channelId;
            this.role         = role;
            this.mediaOption  = mediaOption;
            this.metadata     = metadata;
            this.capability   = capability;

            InitializeSocket();
        }

        void InitializeSocket()
        {
            socket = new MessageWebSocket();

            socket.Control.MessageType = SocketMessageType.Utf8;

            socket.MessageReceived += Socket_MessageReceived;
            socket.Closed          += Socket_Closed;
        }

        void CloseSocket()
        {
            Logger.Debug("Signaling", "CloseSocket");

            if (socket == null)
            {
                Logger.Debug("Signaling", "CloseSocket - already closed");
                return;
            }
            socket.Dispose();
            socket = null;
        }

        public async void Connect()
        {
            Logger.Debug("Signaling", "Connect");

            if (closed)
            {
                Logger.Debug("Signaling", "already closed");
                return;
            }

            try
            {
                await socket.ConnectAsync(new Uri($"{endpoint}?channel_id={channelId}"));
                socketWriter = new DataWriter(socket.OutputStream);

                OnConnect?.Invoke();

                await SendConnectMessage();

            }
            catch (Exception ex)
            {
                Logger.Debug("Signaling", $"failed to connect websocket: {ex.Message}");
                OnError?.Invoke(ErrorType.SocketCantConnect);
            }
        }

        public void Disconnect()
        {
            if (closed)
            {
                Logger.Debug("Signaling", "already closed");
                return;
            }
            closed = true;
            CloseSocket();
            OnDisconnect?.Invoke();
        }

        #region Sending Messages

        async Task SendConnectMessage()
        {
            Logger.Debug("Signaling", "--> connect");
            Logger.Debug("Signaling", capability);

            var msg = MessageConverter.BuildConnectMessage(role, 
                channelId, mediaOption, metadata, capability);

            await SendMessage(msg);
        }

        public async Task SendAnswerMessage(string sdp)
        {
            Logger.Debug("Signaling", "--> answer");

            if (closed)
            {
                Logger.Debug("Signaling", "already closed");
                return;
            }

            Logger.Debug("Signaling", sdp);

            var msg = MessageConverter.BuildAnswerMessage(sdp);

            await SendMessage(msg);
        }

        public async Task SendReAnswerMessage(string sdp)
        {
            Logger.Debug("Signaling", "--> re-answer");

            if (closed)
            {
                Logger.Debug("Signaling", "already closed");
                return;
            }

            Logger.Debug("Signaling", sdp);

            var msg = MessageConverter.BuildReAnswerMessage(sdp);

            await SendMessage(msg);
        }

        public async Task SendUpdateAnswerMessage(string sdp)
        {
            Logger.Debug("Signaling", "--> re-answer(update)");

            if (closed)
            {
                Logger.Debug("Signaling", "already closed");
                return;
            }

            Logger.Debug("Signaling", sdp);

            var msg = MessageConverter.BuildUpdateAnswerMessage(sdp);

            await SendMessage(msg);
        }

        public async Task SendCandidateMessage(string sdp)
        {
            Logger.Debug("Signaling", "--> candidate");

            if (closed)
            {
                Logger.Debug("Signaling", "already closed");
                return;
            }

            Logger.Debug("Signaling", sdp);

            var msg = MessageConverter.BuildCandidateMessage(sdp);

            await SendMessage(msg);
        }

        async Task SendMessage(string message)
        {
            Logger.Debug("Signaling", $"SendMessage: {message}");

           // using (var writer = new DataWriter(socket.OutputStream))
            //{:1
                socketWriter.WriteString(message);
                await socketWriter.StoreAsync();
                //writer.DetachStream();
            //}
        }

        #endregion

        #region Receiving Messages

        void HandleReceivedMessage(string message)
        {
            Logger.Debug("Signaling", "HandleReceivedMessage: " + message);

            string messageType = MessageConverter.ParseType(message);
            switch (messageType)
            {
                case "offer":
                    OnOfferMessage(message);
                    break;
                case "ping":
                    OnPingMessage();
                    break;
                case "update":
                    OnUpdateMessage(message);
                    break;
                case "re-offer":
                    OnReOfferMessage(message);
                    break;
                case "notify":
                    OnNotifyMessage(message);
                    break;
                case "push":
                    OnPushMessage(message);
                    break;
                default:
                    Logger.Debug("Signaling", "Received unknown message-type");
                    break;
            }
        }

        void OnOfferMessage(string message)
        {
            Logger.Debug("Signaling", "<-- offer");

            var offer = MessageConverter.ParseOfferMessage(message);

            Logger.Debug("Signaling", offer.Sdp);

            OnInitialOffer?.Invoke(offer.ClientId, offer.Sdp, offer.Config);
        }

        void OnReOfferMessage(string message)
        {
            Logger.Debug("Signaling", "<-- re-offer");

            var update = MessageConverter.ParseReOfferMessage(message);

            Logger.Debug("Signaling", update.Sdp);

            OnReOffer?.Invoke(update.Sdp);
        }

        void OnUpdateMessage(string message)
        {
            Logger.Debug("Signaling", "<-- re-offer(update)");

            var update = MessageConverter.ParseUpdateMessage(message);

            Logger.Debug("Signaling", update.Sdp);

            OnUpdatedOffer?.Invoke(update.Sdp);
        }

        void OnPingMessage()
        {
            Logger.Debug("Signaling", "<-- ping");

            Logger.Debug("Signaling", "--> pong");

            var pong = MessageConverter.BuildPongMessage();
            var _async = SendMessage(pong);

        }

        void OnNotifyMessage(string message)
        {
            Logger.Debug("Signaling", "<-- notify");
            Logger.Debug("Signaling", "not supported yet");
        }

        void OnPushMessage(string message)
        {
            Logger.Debug("Signaling", "<-- push");
            Logger.Debug("Signaling", "not supported yet");
        }

        #endregion

        #region MessageWebSocket Events

        void Socket_MessageReceived(MessageWebSocket sender, 
            MessageWebSocketMessageReceivedEventArgs args)
        {
            Logger.Debug("Signaling", "MessageReceived");

            try
            {
                using (DataReader reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = UnicodeEncoding.Utf8;
                    string msg = reader.ReadString(reader.UnconsumedBufferLength);
                    HandleReceivedMessage(msg);
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("Signaling", $"failed to handle received message: {ex.Message}");
                Disconnect();
            }
        }

        void Socket_Closed(IWebSocket sender, 
            WebSocketClosedEventArgs args)
        {
            Logger.Debug("Signaling", $"Closed code-{args.Code}, reason-{args.Reason}");
            Disconnect();
        }

        #endregion
    }
}
