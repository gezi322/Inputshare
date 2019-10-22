using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.NetIpc.Messages
{
    [Serializable]
    public class NetIpcStateResponseMessage : IpcMessage
    {
        public NetIpcStateResponseMessage(Guid messageId, bool connected) : base(IpcMessageType.NetIpcStateResponse, messageId)
        {
            Connected = connected;
        }

        public bool Connected { get; }
    }
}
