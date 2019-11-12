using System;
using System.IO;

namespace InputshareLib.Net.Messages
{
    internal class FileStreamReadResponseMessage : NetworkMessage
    {
        public FileStreamReadResponseMessage(byte[] readData, Guid messageId = default) : base(MessageType.FileStreamReadResponse, messageId)
        {
            ReadData = readData;
        }

        public FileStreamReadResponseMessage(byte[] data) : base(data)
        {
            using (MemoryStream ms = new MemoryStream(data, 21, data.Length - 21))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    int readLen = br.ReadInt32();
                    ReadData = br.ReadBytes(readLen);
                }
            }
        }

        public override byte[] ToBytes()
        {
            byte[] data = WritePacketInfo(this, 16 + 4 + ReadData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(ReadData.Length), 0, data, 21, 4);
            Buffer.BlockCopy(ReadData, 0, data, 21 + 4, ReadData.Length);
            return data;
        }
        public byte[] ReadData { get; }
    }
}
