using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.NetIpc.Messages
{
    [Serializable]
    public class NetIpcAutoReconnectResponseMessage : IpcMessage
    {
        public NetIpcAutoReconnectResponseMessage(bool enabled, Guid messageId) : base(IpcMessageType.NetIpcAutoReconnectResponse, messageId)
        {
            Enabled = enabled;
        }

        public bool Enabled { get; }
    }
}
