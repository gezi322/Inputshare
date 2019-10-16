using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.NamedIpc.Messages
{
    [Serializable]
    public class NamedIpcConnectMessage : NamedIpcMessage
    {
        public NamedIpcConnectMessage(string address, int port, string clientName, Guid messageId = default) : base(NamedIpcMessageType.Connect, messageId)
        {
            Address = address;
            Port = port;
            ClientName = clientName;
        }

        public string Address { get; }
        public int Port { get; }
        public string ClientName { get; }
    }
}
