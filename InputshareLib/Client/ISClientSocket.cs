﻿using InputshareLib.Net;
using InputshareLib.Net.Messages;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace InputshareLib.Client
{
    internal class ISClientSocket : NetworkSocket
    {
        /// <summary>
        /// Occurs when a connection attempt fails
        /// </summary>
        public event EventHandler<string> ConnectionFailed;
        /// <summary>
        /// Occurs when a connection attempt is successful
        /// </summary>
        public event EventHandler Connected;



        /// <summary>
        /// Occurs when this client is no longer the input client,
        /// or when this client is the input client.
        /// </summary>
        public event EventHandler<bool> ActiveClientChanged;
        
        /// <summary>
        /// Occurs when the server assigns a client at an edge of this
        /// client
        /// </summary>
        public event EventHandler<BoundEdges> EdgesChanged;

        /// <summary>
        /// Address of the current connected server
        /// </summary>
        public IPEndPoint ServerAddress { get; private set; }

        /// <summary>
        /// Fires after a certain ammount of time after a connection attempt
        /// and checks whether the server
        /// has sent a response, if not then it closes the connection.
        /// </summary>
        private Timer serverReplyTimer;

        /// <summary>
        /// Stores the name,guid and display config for this client
        /// </summary>
        private ConnectionInfo conInfo;

        private bool serverResponded = false;

        public event EventHandler CancelAnyDragDrop;

        public ISClientSocket()
        {

        }

        /// <summary>
        /// Attemts to connect to the specified server with the specified
        /// name and ID. Cancels any ongoing connection or connection attempt.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="info"></param>
        public void Connect(string address, int port, ConnectionInfo info)
        {
            if (IsConnected)
                throw new InvalidOperationException("Socket already connected");

            if (!IPAddress.TryParse(address, out IPAddress destAddr))
                throw new ArgumentException("Invalid address");

            if (port < 1 || port > 65535)
                throw new ArgumentException("Invalid port");

            conInfo = info;

            tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true //Disable nagles algorithm!
            };

            tcpSocket.BeginConnect(new IPEndPoint(destAddr, port), TcpSocket_ConnectCallback, null);
            serverReplyTimer = new Timer(ServerReplyTimerCallback, null, 5000, 0);
        }

        /// <summary>
        /// Disonnect from the server, optionally notifies the server
        /// </summary>
        /// <param name="notify"></param>
        public void Disconnect(bool notify)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Socket not connected");

            if (notify)
                SendMessage(new NetworkMessage(MessageType.ClientDisconnecting));

            Close();
        }

        /// <summary>
        /// Closes the socket
        /// </summary>
        public override void Close()
        {
            serverResponded = true;
            serverReplyTimer?.Dispose();
            base.Close();
        }

        /// <summary>
        /// Fired after a connection attempt to check whether the
        /// server has sent a reply
        /// </summary>
        /// <param name="sync"></param>
        private void ServerReplyTimerCallback(object sync)
        {
            if (!base.IsConnected && !serverResponded)
                HandleConnectedFailed("Timed out waiting for server reponse");

            serverReplyTimer.Dispose();
        }

        /// <summary>
        /// Sends the client name and guid to the server. Called when connection is first established
        /// </summary>
        /// <param name="clientName"></param>
        /// <param name="clientId"></param>
        private void SendInitialInfo()
        {
            SendMessage(new ClientInitialMessage(conInfo.Name, conInfo.Id, conInfo.DisplayConf, Settings.InputshareVersion));
        }

        /// <summary>
        /// Send the specified display config to server
        /// </summary>
        /// <param name="displayData"></param>
        public void SendDisplayConfig(byte[] displayData)
        {
            SendMessage(new DisplayConfigMessage(displayData, Guid.NewGuid()));
        }

        /// <summary>
        /// notifies
        /// </summary>
        /// <param name="edge"></param>
        public void SendEdgeHit(Edge edge)
        {
            switch (edge)
            {
                case Edge.Bottom:
                    SendMessage(new NetworkMessage(MessageType.EdgeHitBottom));
                    break;
                case Edge.Top:
                    SendMessage(new NetworkMessage(MessageType.EdgeHitTop));
                    break;
                case Edge.Left:
                    SendMessage(new NetworkMessage(MessageType.EdgeHitLeft));
                    break;
                case Edge.Right:
                    SendMessage(new NetworkMessage(MessageType.EdgeHitRight));
                    break;
            }
        }

        /// <summary>
        /// Handles messages received by the server
        /// </summary>
        /// <param name="data"></param>
        protected override void HandleMessage(byte[] data)
        {
            base.HandleMessage(data);
            MessageType type = (MessageType)data[4];
            if (type == MessageType.ServerRequestInitialInfo)
            {
                HandleRequestInitialInfo();
            }
            else if (type == MessageType.ServerOK)
            {
                HandleServerOK();
            }
            else if (type == MessageType.ClientDeclined)
            {
                HandleClientDeclinedMessage(new ClientDeclinedMessage(data));
            }
            else if (type == MessageType.ClientActive)
            {
                ActiveClientChanged?.Invoke(this, true);
                return;
            }else if(type == MessageType.ClientInactive)
            {
                ActiveClientChanged?.Invoke(this, false);
                return;
            }else if(type == MessageType.ClientEdgeStates)
            {
                HandleEdgesChangedMessage(new ClientEdgesStateMessage(data));
            }else if(type == MessageType.CancelAnyDragDrop)
            {
                CancelAnyDragDrop?.Invoke(this, null);
            }
                
        }

        /// <summary>
        /// Occurs when the server assigns or unassigns a client to an edge of this client
        /// </summary>
        /// <param name="message"></param>
        private void HandleEdgesChangedMessage(ClientEdgesStateMessage message)
        {
            EdgesChanged?.Invoke(this, new BoundEdges(
                message.ClientTop,
                message.ClientBottom,
                message.ClientLeft,
                message.ClientRight
                ));
        }

        

        private void HandleServerOK()
        {
            ServerAddress = tcpSocket.RemoteEndPoint as IPEndPoint;
            serverResponded = true;
            base.IsConnected = true;
            Connected?.Invoke(this, null);
        }

        private void HandleRequestInitialInfo()
        {
            SendInitialInfo();
        }

        private void HandleClientDeclinedMessage(ClientDeclinedMessage message)
        {
            serverResponded = true;
            ConnectionFailed?.Invoke(this, "The server declined the connection '" + message.Reason + "'");
            errorHandled = true;
        }

        protected override void HandleConnectionClosed(string error)
        {
            serverResponded = true;
            base.HandleConnectionClosed(error);
        }

        protected override void HandleConnectedFailed(string error)
        {
            base.IsConnected = false;
            errorHandled = true;
            base.HandleConnectedFailed(error);
            ConnectionFailed?.Invoke(this, error);
        }

        protected override void OnConnected()
        {
            base.OnConnected();
        }

        public struct ConnectionInfo
        {
            public ConnectionInfo(string name, Guid id, byte[] displayConf)
            {
                Name = name;
                Id = id;
                DisplayConf = displayConf;
            }

            public string Name { get; }
            public Guid Id { get; }
            public byte[] DisplayConf { get; }
        }

        public struct BoundEdges
        {
            public BoundEdges(bool top, bool bottom, bool left, bool right)
            {
                Top = top;
                Bottom = bottom;
                Left = left;
                Right = right;
            }

            public bool Top { get; }
            public bool Bottom { get; }
            public bool Left { get; }
            public bool Right { get; }
        }
    }
}
