using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using InputshareLib;
using InputshareLib.Client;
using InputshareLibWindows.IPC.NetIpc.Messages;

namespace InputshareLibWindows.IPC.NetIpc
{

    /// <summary>
    /// NetIpcHost is used to communicate with the inputsharewindows app running in user space
    /// 
    /// TODO - This should be using named pipes for to allow only administrators to connect.
    /// </summary>
    public class NetIpcHost : IpcBase
    {
        /// <summary>
        /// The socket that accepts client connections (limited to one client)
        /// </summary>
        private Socket listenSocket;

        /// <summary>
        /// Socket used to communicate with application
        /// </summary>
        private Socket clientSocket;

        /// <summary>
        /// The inputshare client 
        /// </summary>
        private ISClient client;

        public NetIpcHost(ISClient clientInstance, string conName) : base(true, conName, false)
        {
            ISLogger.LogMessageOut += (object s, string msg) => { SendLogMessage(msg); };

            client = clientInstance;
            client.Connected += Client_Connected;
            client.ConnectionError += Client_ConnectionError;
            client.ConnectionFailed += Client_ConnectionFailed;
            client.Disconnected += Client_Disconnected;

            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 56578));
            listenSocket.Listen(1);

            ISLogger.Write("NetIpcHost: Waiting on port 56578");

            listenSocket.BeginAccept(AcceptCallback, listenSocket);
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            NotifyDisconnected();
        }

        private void Client_ConnectionFailed(object sender, string e)
        {
            NotifyDisconnected();
        }

        private void Client_ConnectionError(object sender, string e)
        {
            NotifyDisconnected();
        }

        private void Client_Connected(object sender, IPEndPoint e)
        {
            NotifyConnected();
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket soc = (Socket)ar.AsyncState;
                clientSocket = soc.EndAccept(ar);

                Start(new NetworkStream(clientSocket));
            }
            catch(Exception ex)
            {
                ISLogger.Write("NetIpcHost: Failed to accept socket: " + ex.Message);

                if(!disposedValue)
                    listenSocket.BeginAccept(AcceptCallback, listenSocket);
            }
        }

        private void SendLogMessage(string message)
        {
            if (IsConnected)
                Write(new NetIpcLogMessage(message));
        }

        protected override void OnError(Exception ex)
        {
            clientSocket?.Dispose();
            if (!disposedValue)
            {
                listenSocket.BeginAccept(AcceptCallback, listenSocket);
                OnDisconnect(ex.Message);
            }
        }

        protected override void ProcessMessage(IpcMessage message)
        {
            if (message.MessageType == IpcMessageType.NetIpcStateRequest)
                HandleStateRequest(message);
            else if (message is NetIpcConnectMessage conMsg)
                HandleConnectMessage(conMsg);
            else if (message.MessageType == IpcMessageType.NetIpcNameRequest)
                HandleNameRequest(message);
            else if (message.MessageType == IpcMessageType.NetIpcDisconnect)
                HandleDisconnect();
            else if (message.MessageType == IpcMessageType.NetIpcAddressRequest)
                HandleAddressRequest(message);
            else if (message is NetIpcSetNameMessage nameMsg)
                HandleSetName(nameMsg);
            else if (message.MessageType == IpcMessageType.NetIpcEnableAutoReconnect)
                HandleSetAutoReconnect(true);
            else if (message.MessageType == IpcMessageType.NetIpcDisableAutoReconnect)
                HandleSetAutoReconnect(false);
            else if (message.MessageType == IpcMessageType.NetIpcAutoReconnectRequest)
                HandleGetAutoReconnect(message);  
        }

        private void HandleSetAutoReconnect(bool enabled)
        {
            client.AutoReconnect = enabled;
            Write(new NetIpcAutoReconnectResponseMessage(enabled, Guid.NewGuid()));
            SendLogMessage("Auto reconnect enabled: " + enabled);
        }

        private void HandleGetAutoReconnect(IpcMessage message)
        {
            Write(new NetIpcAutoReconnectResponseMessage(client.AutoReconnect, message.MessageId));
        }

        private void HandleSetName(NetIpcSetNameMessage message)
        {
            client.ClientName = message.Name;
            SendLogMessage("Name set to " + client.ClientName);
        }


        private void HandleAddressRequest(IpcMessage message)
        {
            if (client.ServerAddress != null)
                Write(new NetIpcConnectedAddressResponseMessage(client.ServerAddress, message.MessageId));
            else
                Write(new NetIpcConnectedAddressResponseMessage(new IPEndPoint(IPAddress.Any, 0 ), message.MessageId));
        }

        private void HandleConnectMessage(NetIpcConnectMessage message)
        {
            if (!client.IsConnected)
                client.Connect(message.Address);
        }

        private void HandleNameRequest(IpcMessage message)
        {
            Write(new NetIpcClientNameResponseMessage(client.ClientName, message.MessageId));
        }

        private void HandleDisconnect()
        {
            if (client.IsConnected)
                client.Disconnect();
            else
                SendLogMessage("Cannot disconnect: already disconnected");
        }

        public void NotifyDisconnected()
        {
            if(IsConnected)
                Write(new IpcMessage(IpcMessageType.NetIpcClientDisconnected));
        }

        public void NotifyConnected()
        {
            if(IsConnected)
                Write(new IpcMessage(IpcMessageType.NetIpcClientConnected));
        }

        private void HandleStateRequest(IpcMessage message)
        {
            Write(new NetIpcStateResponseMessage(message.MessageId, client.IsConnected));
        }

        protected override void Dispose(bool disposing)
        {
            listenSocket.Dispose();
            clientSocket.Dispose();
            base.Dispose(disposing);
        }
    }
}
