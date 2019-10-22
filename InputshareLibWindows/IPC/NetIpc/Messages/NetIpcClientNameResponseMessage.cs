using System;

namespace InputshareLibWindows.IPC.NetIpc.Messages
{
    [Serializable]
    public class NetIpcClientNameResponseMessage : IpcMessage
    {
        public NetIpcClientNameResponseMessage(string clientName, Guid messageId) : base(IpcMessageType.NetIpcNameResponse, messageId)
        {
            ClientName = clientName;
        }

        public string ClientName { get; }
    }
}
