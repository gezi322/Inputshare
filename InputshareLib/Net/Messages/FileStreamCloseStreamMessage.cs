using System;

namespace InputshareLib.Net.Messages
{
    internal class FileStreamCloseStreamMessage : NetworkMessage
    {
        public FileStreamCloseStreamMessage(byte[] data) : base(data)
        {
            byte[] tokenData = new byte[16];
            Buffer.BlockCopy(data, 21, tokenData, 0, 16);
            Token = new Guid(tokenData);

            byte[] fileIdData = new byte[16];
            Buffer.BlockCopy(data, 21 + 16, fileIdData, 0, 16);
            FileId = new Guid(fileIdData);
        }

        public override byte[] ToBytes()
        {
            byte[] data = WritePacketInfo(this, 32);
            Buffer.BlockCopy(Token.ToByteArray(), 0, data, 21, 16);
            Buffer.BlockCopy(FileId.ToByteArray(), 0, data, 21 + 16, 16);
            return data;
        }

        public FileStreamCloseStreamMessage(Guid token, Guid fileId, Guid messageId = default) : base(MessageType.FileStreamCloseRequest, messageId)
        {
            Token = token;
            FileId = fileId;
        }

        public Guid Token { get; }
        public Guid FileId { get; }
    }
}
