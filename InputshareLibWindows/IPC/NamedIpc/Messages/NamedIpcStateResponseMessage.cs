using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace InputshareLibWindows.IPC.NamedIpc.Messages
{
    [Serializable]
    public class NamedIpcStateResponseMessage : NamedIpcMessage
    {
        public NamedIpcStateResponseMessage(Guid messageId, bool connected, string connectedAddress, string name, Guid clientId, bool autoReconnect) : base(NamedIpcMessageType.StateResponse, messageId)
        {
            Connected = connected;
            ConnectedAddress = connectedAddress;
            Name = name;
            AutoReconnect = autoReconnect;
            ClientId = clientId;
        }

        public bool Connected { get; }
        public string ConnectedAddress { get; }
        public string Name { get; }
        public bool AutoReconnect { get; }
        public Guid ClientId { get; }
    }
}
