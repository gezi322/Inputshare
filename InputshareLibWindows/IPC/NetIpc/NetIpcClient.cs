using InputshareLib;
using InputshareLibWindows.IPC.NetIpc.Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLibWindows.IPC.NetIpc
{
    /// <summary>
    /// IPC client using sockets to allow the inputshare desktop app to connect to the service
    /// </summary>
    public class NetIpcClient : IpcBase
    {
        public event EventHandler ServerConnected;
        public event EventHandler ServerDisconnected;
        public event EventHandler<bool> AutoReconnectChanged;
        public event EventHandler<string> ServiceLogMessage;

        private Socket clientSocket;

        public NetIpcClient(string conName) : base(true, conName, false)
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(new IPEndPoint(IPAddress.Loopback, 56578));
            Start(new NetworkStream(clientSocket));
            Write(new IpcMessage(IpcMessageType.IpcClientOK));
        }

        public void Connect(IPEndPoint address)
        {
            Write(new NetIpcConnectMessage(address));
        }

        public void Disconnect()
        {
            Write(new IpcMessage(IpcMessageType.NetIpcDisconnect));
        }

        public void SetAutoReconnectEnable(bool enable)
        {
            if (enable)
                Write(new IpcMessage(IpcMessageType.NetIpcEnableAutoReconnect));
            else
                Write(new IpcMessage(IpcMessageType.NetIpcDisableAutoReconnect));
        }

        public async Task<string> GetClientNameAsync()
        {
            NetIpcClientNameResponseMessage msg = (NetIpcClientNameResponseMessage)await SendRequest(new IpcMessage(IpcMessageType.NetIpcNameRequest), IpcMessageType.NetIpcNameResponse);
            return msg.ClientName;
        }

        public async Task<bool> GetConnectedStateAsync()
        {
            NetIpcStateResponseMessage msg = (NetIpcStateResponseMessage)await SendRequest(new IpcMessage(IpcMessageType.NetIpcStateRequest), IpcMessageType.NetIpcStateResponse);
            return msg.Connected;
        }

        public async Task<bool> GetAutoReconnectStateAsync()
        {
            NetIpcAutoReconnectResponseMessage msg = (NetIpcAutoReconnectResponseMessage)await SendRequest(new IpcMessage(IpcMessageType.NetIpcAutoReconnectRequest), IpcMessageType.NetIpcAutoReconnectResponse);
            return msg.Enabled;
        }

        public void SetName(string clientName)
        {
            Write(new NetIpcSetNameMessage(clientName));
        }

        public async Task<IPEndPoint> GetConnectedAddressAsync()
        {
            NetIpcConnectedAddressResponseMessage msg = (NetIpcConnectedAddressResponseMessage)await SendRequest(new IpcMessage(IpcMessageType.NetIpcAddressRequest), IpcMessageType.NetIpcAddressResponse);
            return msg.Address;
        }

        protected override void ProcessMessage(IpcMessage message)
        {
            if (message is NetIpcLogMessage logMsg)
                HandleLogMessage(logMsg);
            else if (message.MessageType == IpcMessageType.NetIpcClientDisconnected)
                ServerDisconnected?.Invoke(this, null);
            else if (message.MessageType == IpcMessageType.NetIpcClientConnected)
                ServerConnected?.Invoke(this, null);
            else if (message is NetIpcAutoReconnectResponseMessage autoMsg)
                HandleAutoReconnectStateMessage(autoMsg);
        }
        
        private void HandleAutoReconnectStateMessage(NetIpcAutoReconnectResponseMessage message)
        {
            AutoReconnectChanged?.Invoke(this, message.Enabled);
        }
        
        private void HandleLogMessage(NetIpcLogMessage message)
        {
            ServiceLogMessage?.Invoke(this, message.Message);
        }

        protected override void Dispose(bool disposing)
        {
            clientSocket?.Dispose();
            base.Dispose(disposing);
        }

    }
}
