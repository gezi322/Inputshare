using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.AnonIpc.Messages
{
    public sealed class AnonIpcReadStreamResponseMessage : IpcMessage
    {
        public byte[] ResponseData { get; }

        public AnonIpcReadStreamResponseMessage(byte[] data) : base(data)
        {
            ResponseData = new byte[data.Length - 17];
            Buffer.BlockCopy(data, 17, ResponseData, 0, ResponseData.Length);
        }

        public override byte[] ToBytes()
        {
            byte[] data = CreateArray(ResponseData.Length);
            Buffer.BlockCopy(ResponseData, 0, data, 17, ResponseData.Length);
            return data;
        }

        public AnonIpcReadStreamResponseMessage(byte[] responseData, Guid messageId = default) : base(IpcMessageType.AnonIpcStreamReadResponse, messageId)
        {
            ResponseData = responseData;
        }
    }
}
