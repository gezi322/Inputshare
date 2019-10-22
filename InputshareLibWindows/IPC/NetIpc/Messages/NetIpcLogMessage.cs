using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.NetIpc.Messages
{
    [Serializable]
    public class NetIpcLogMessage : IpcMessage
    {
        public NetIpcLogMessage(string message) : base(IpcMessageType.NetIpcLogMessage)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
