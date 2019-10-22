using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace InputshareLibWindows.IPC.NetIpc.Messages
{
    [Serializable]
    public class NetIpcConnectMessage : IpcMessage
    {
        public NetIpcConnectMessage(IPEndPoint address) : base(IpcMessageType.NetIpcConnect)
        {
            Address = address;
        }

        private string _address;

        public IPEndPoint Address { get => IPEndPoint.Parse(_address); set => _address = value.ToString(); }
    }
}
