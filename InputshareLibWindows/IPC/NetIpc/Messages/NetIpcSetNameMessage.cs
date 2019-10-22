using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.NetIpc.Messages
{
    [Serializable]
    public class NetIpcSetNameMessage : IpcMessage
    {
        public NetIpcSetNameMessage(string name) : base(IpcMessageType.NetIpcSetName)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
