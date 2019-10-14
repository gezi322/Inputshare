using InputshareLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.AnonIpc.Messages
{
    public class AnonIpcReadStreamRequestMessage : AnonIpcMessage
    {
        public AnonIpcReadStreamRequestMessage(byte[] data) : base(data)
        {
            ReadLen = data.ParseInt(17);
            Token = data.ParseGuid(21);
            FileId = data.ParseGuid(21 + 16);
        }

        public override byte[] ToBytes()
        {
            byte[] data = CreateArray(36);
            data.InsertInt(17, ReadLen);
            data.InsertGuid(Token, 21);
            data.InsertGuid(FileId, 21 + 16);
            return data;
        }

        public AnonIpcReadStreamRequestMessage(Guid token, Guid fileId, int readLen, Guid messageId = default) : base(AnonIpcMessageType.StreamReadRequest, messageId)
        {
            Token = token;
            FileId = fileId;
            ReadLen = readLen;
        }

        public Guid Token { get; }
        public Guid FileId { get; }
        public int ReadLen { get; }
    }
}
