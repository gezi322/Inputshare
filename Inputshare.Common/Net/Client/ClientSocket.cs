﻿using Inputshare.Common.Net.Messages;
using Inputshare.Common.Net.Messages.Replies;
using Inputshare.Common.Net.Messages.Requests;
using Inputshare.Common.Net.RFS;
using Inputshare.Common.Net.RFS.Client;
using Inputshare.Common.Net.UDP;
using Inputshare.Common.Net.UDP.Messages;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Inputshare.Common.Net.Client
{
    internal class ClientSocket : SocketBase
    {
        internal event EventHandler<Exception> Disconnected;
        internal event EventHandler<bool> InputClientChanged;
        internal event EventHandler<ClientSidesChangedArgs> SideStateChanged;
        internal Rectangle VirtualBounds { get; private set; }
        internal ClientSocketState State => _state;
        private ClientSocketState _state = ClientSocketState.Idle;

        private SemaphoreSlim _connectSemaphore;
        private Socket _client;
        private bool _disconnecting;
        private ClientUdpSocket _udpSocket;
        private bool _bindUdp;

        internal ClientSocket(RFSController fileController, bool bindUdp) : base(fileController)
        {
            _bindUdp = bindUdp;
        }

        /// <summary>
        /// Connects to a server
        /// </summary>
        /// <param name="address"></param>
        /// <param name="timeout"></param>
        /// <returns>True if connected</returns>
        /// <exception cref="NetConnectionFailedException"/>
        internal async Task<bool> ConnectAsync(ClientConnectArgs args, int timeout = 1000)
        {
            if (_state != ClientSocketState.Idle)
                throw new InvalidOperationException("Cannot connect when state is " + _state);

            try
            {
                _disconnecting = false;
                _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                _state = ClientSocketState.AttemptingConnection;
                Logger.Write($"Connecting to {args.Address}");

                //Create a waithandle that will release when we receive a NetServerConnectionMessage
                _connectSemaphore = new SemaphoreSlim(0, 1);

                //Attempt to connect
                await _client.ConnectAsync(args.Address);

                //Start a background task to receive data
                BeginReceiveData(_client);

                if (_bindUdp)
                    CreateUdpClient(args.Address);

                //Send a initial message to the server containing information about this client
                await SendConnectionInfoAsync(args, _udpSocket == null ? 0 : _udpSocket.BindAddress.Port);

                //don't return until we receive a ServerConnection message, or throw if timeout reached
                if (!await _connectSemaphore.WaitAsync(timeout))
                {
                    _state = ClientSocketState.Idle;
                    Dispose();

                    throw new NetConnectionFailedException("Connection timed out");
                }

                Logger.Write($"Connected to {args.Address}");
                _state = ClientSocketState.Connected;
                return true;
            }catch(Exception ex)
            {
                _state = ClientSocketState.Idle;
                Logger.Write("Failed to connect: " + ex.Message);
                return false;
            }
            
        }

        private void CreateUdpClient(IPEndPoint serverAddress)
        {
            _udpSocket = ClientUdpSocket.Create(serverAddress);
            _udpSocket.RegisterHandlerForAddress(serverAddress, new UdpBase.UdpMessageHandler(HandleUdpMessage));
        }

        private void HandleUdpMessage(IUdpMessage message)
        {
            if (message.Type == UdpMessageType.ServerOK)
                _udpSocket.SendToServer(new UdpGenericMessage(UdpMessageType.ClientOK));
            else if (message is UdpInputMessage inputMessage)
                RaiseInputReceived(inputMessage.Input);
        }

        /// <summary>
        /// Disconnects from the server
        /// </summary>
        internal override void DisconnectSocket()
        {
            if (_state == ClientSocketState.Idle)
                throw new InvalidOperationException("Cannot disconnect when state is Idle");

            _udpSocket?.Dispose();
            _disconnecting = true;
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

        private async Task SendConnectionInfoAsync(ClientConnectArgs args, int udpPort)
        {
            await SendMessageAsync(new NetClientConnectionMessage(args.Name, args.Id, "0.1", args.VirtualBounds, udpPort));
        }

        protected override void HandleException(Exception ex)
        {
            //Dont throw an exception if we called disconnectsocket
            if(_state == ClientSocketState.Connected && !_disconnecting)
            {
                _udpSocket?.Dispose();
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

        protected override void HandleRequest(NetRequestBase request)
        {
            if (request is NetNameRequest)
                SendMessage(new NetNameReply("Hello world", request.MessageId));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _udpSocket?.Dispose();
                _state = ClientSocketState.Idle;
                _connectSemaphore?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
