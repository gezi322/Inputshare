using InputshareLib.Net.Messages;
using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib.Net.Client
{
    internal class ClientSocket : SocketBase
    {
        internal event EventHandler<Exception> Disconnected;
        internal event EventHandler<bool> InputClientChanged;
        internal event EventHandler<ScreenshotRequestArgs> ScreenshotRequested;
        internal event EventHandler<ClientSidesChangedArgs> SideStateChanged;
        internal Rectangle VirtualBounds { get; private set; }
        internal ClientSocketState State => _state;
        private ClientSocketState _state;

        private SemaphoreSlim _connectSemaphore;
        private Socket _client;

        /// <summary>
        /// Connects to a server
        /// </summary>
        /// <param name="address"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="NetConnectionFailedException"/>
        internal async Task ConnectAsync(ClientConnectArgs args, int timeout = 5000)
        {
            if (_state != ClientSocketState.Idle)
                throw new InvalidOperationException("Cannot connect when state is " + _state);

            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _state = ClientSocketState.AttemptingConnection;
            Logger.Write($"Connecting to {args.Address}");

            //Create a waithandle that will release when we receive a NetServerConnectionMessage
            _connectSemaphore = new SemaphoreSlim(0, 1);

            //Attempt to connect
            await _client.ConnectAsync(args.Address);

            //Start a background task to receive data
            BeginReceiveData(_client);

            //Send a initial message to the server containing information about this client
            await SendConnectionInfoAsync(args);

            //don't return until we receive a ServerConnection message, or throw if timeout reached
            if (!await _connectSemaphore.WaitAsync(timeout))
            {
                Dispose();
                _state = ClientSocketState.Idle;
                throw new NetConnectionFailedException("Connection timed out");
            }

            Logger.Write($"Connected to {args.Address}");
            _state = ClientSocketState.Connected;
        }

        /// <summary>
        /// Disconnects from the server
        /// </summary>
        internal override void DisconnectSocket()
        {
            if (_state == ClientSocketState.Idle)
                throw new InvalidOperationException("Cannot disconnect when state is Idle");


            base.DisconnectSocket();
            _state = ClientSocketState.Idle;
            Logger.Write("Disconnected");
        }

        internal async Task SendDisplayUpdateAsync(Rectangle newBounds)
        {
            await SendMessageAsync(new NetDisplayBoundsUpdateMessage(newBounds));
        }

        internal async Task SendSideHitAsync(Side side, int posX, int posY)
        {
            await SendMessageAsync(new NetSideHitMessage(side, posX, posY));
        }

        private async Task SendConnectionInfoAsync(ClientConnectArgs args)
        {
            await SendMessageAsync(new NetClientConnectionMessage(args.Name, args.Id, "0.1", args.VirtualBounds));
        }

        protected override void HandleException(Exception ex)
        {
            if(_state == ClientSocketState.Connected)
            {
                _state = ClientSocketState.Idle;
                Disconnected?.Invoke(this, ex);
            }
        }

        protected override void HandleGenericMessage(NetMessageBase message)
        {
            if(message is NetServerConnectionMessage && _state == ClientSocketState.AttemptingConnection)
            {
                _connectSemaphore.Release();
            }else if(message is NetInputClientStateMessage inputMessage)
            {
                InputClientChanged?.Invoke(this, inputMessage.InputClient);
            }else if(message is NetClientSideStateMessage sideMsg)
            {
                SideStateChanged?.Invoke(this, new ClientSidesChangedArgs(sideMsg.Left, sideMsg.Right, sideMsg.Top, sideMsg.Bottom));
            }
        }

        protected override async Task HandleRequestAsync(NetRequestBase request)
        {
            if (request is NetNameRequest)
                await SendMessageAsync(new NetNameReply("Hello world", request.MessageId));
            else if(request is NetScreenshotRequest)
            {
                ScreenshotRequestArgs sa = new ScreenshotRequestArgs();
                ScreenshotRequested?.Invoke(this, sa);
                await SendMessageAsync(new NetScreenshotReply(request.MessageId, sa.Data));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connectSemaphore?.Dispose();
                _state = ClientSocketState.Idle;
            }

            base.Dispose(disposing);
        }
    }
}
