using InputshareLib.Net.Messages;
using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib.Net.Client
{
    internal class ClientSocket : SocketBase
    {
        internal event EventHandler<Exception> Disconnected;

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
        internal async Task ConnectAsync(IPEndPoint address, int timeout = 5000)
        {
            if (_state != ClientSocketState.Idle)
                throw new InvalidOperationException("Cannot connect when state is " + _state);

            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _state = ClientSocketState.AttemptingConnection;
            Logger.Write($"Connecting to {address}");

            //Create a waithandle that will release when we receive a NetServerConnectionMessage
            _connectSemaphore = new SemaphoreSlim(0, 1);

            //Attempt to connect
            await _client.ConnectAsync(address);

            //Start a background task to receive data
            BeginReceiveData(_client);

            //Send a initial message to the server containing information about this client
            await SendConnectionInfoAsync(Environment.MachineName);

            //don't return until we receive a ServerConnection message, or throw if timeout reached
            if (!await _connectSemaphore.WaitAsync(timeout))
            {
                Dispose();
                _state = ClientSocketState.Idle;
                throw new NetConnectionFailedException("Connection timed out");
            }

            Logger.Write($"Connected to {address}");
            _state = ClientSocketState.Connected;
        }

        internal override void DisconnectSocket()
        {
            if (_state == ClientSocketState.Idle)
                throw new InvalidOperationException("Cannot disconnect when state is Idle");


            base.DisconnectSocket();
            _state = ClientSocketState.Idle;
            Logger.Write("Disconnected");
        }

        private async Task SendConnectionInfoAsync(string name)
        {
            await SendMessageAsync(new NetClientConnectionMessage(name, Guid.NewGuid(), "0.1", new System.Drawing.Rectangle(0, 0, 1024, 768)));
        }

        protected override void HandleException(Exception ex)
        {
            _state = ClientSocketState.Idle;
            Disconnected?.Invoke(this, ex);
        }

        protected override void HandleGenericMessage(NetMessageBase message)
        {
            if(message is NetServerConnectionMessage && _state == ClientSocketState.AttemptingConnection)
            {
                _connectSemaphore.Release();
                return;
            }
        }

        protected override async Task HandleRequestAsync(NetRequestBase request)
        {
            if (request is NetNameRequest)
                await SendMessageAsync(new NetNameReply("Hello world", request.MessageId));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connectSemaphore?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
