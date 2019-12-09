using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib;

namespace InputshareLibWindows.IPC.AnonIpc.Messages
{
    public class AnonIpcRequestFileTokenResponseMessage : IpcMessage
    {
        public Guid AccessToken { get; }

        public AnonIpcRequestFileTokenResponseMessage(byte[] data) : base(data)
        {
            AccessToken = data.ParseGuid(17);
        }

        public override byte[] ToBytes()
        {
            byte[] data = CreateArray(16);
            data.InsertGuid(AccessToken, 17);
            return data;
        }

        public AnonIpcRequestFileTokenResponseMessage(Guid token, Guid messageId = default) : base(IpcMessageType.AnonIpcFileTokenResponse, messageId)
        {
            AccessToken = token;
        }
    }
}
