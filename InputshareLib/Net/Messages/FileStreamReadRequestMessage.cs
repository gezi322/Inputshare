using System;
using System.IO;

namespace InputshareLib.Net.Messages
{
    internal class FileStreamReadRequestMessage : NetworkMessage
    {
        public FileStreamReadRequestMessage(Guid token, Guid fileRequestId, int readSize, Guid messageId = default) : base(MessageType.FileStreamReadRequest, messageId)
        {
            Token = token;
            FileRequestId = fileRequestId;
            ReadSize = readSize;
        }

        public FileStreamReadRequestMessage(byte[] data) : base(data)
        {
            using (MemoryStream ms = new MemoryStream(data, 21, data.Length - 21))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    Token = new Guid(br.ReadBytes(16));
                    FileRequestId = new Guid(br.ReadBytes(16));
                    ReadSize = br.ReadInt32();
                }
            }
        }

        public Guid Token { get; }
        public Guid FileRequestId { get; }
        public int ReadSize { get; }

        public override byte[] ToBytes()
        {
            byte[] data = WritePacketInfo(this, 16 + 16 + 4);
            Buffer.BlockCopy(Token.ToByteArray(), 0, data, 21, 16);
            Buffer.BlockCopy(FileRequestId.ToByteArray(), 0, data, 21 + 16, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(ReadSize), 0, data, 21 + 16 + 16, 4);
            return data;
        }
    }
}
