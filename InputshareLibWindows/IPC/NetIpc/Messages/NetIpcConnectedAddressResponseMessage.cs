using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace InputshareLibWindows.IPC.NetIpc.Messages
{
    [Serializable]
    public class NetIpcConnectedAddressResponseMessage : IpcMessage
    {
        public NetIpcConnectedAddressResponseMessage(IPEndPoint address, Guid messageId) : base(IpcMessageType.NetIpcAddressResponse, messageId)
        {
            Address = address;
        }

        public IPEndPoint Address { get => IPEndPoint.Parse(_address); private set => Address = value; }
        private string _address;
    }
}
