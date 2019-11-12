using System;
using System.IO;
using System.Text;

namespace InputshareLib.Net.Messages
{
    internal class FileErrorMessage : NetworkMessage
    {
        public FileErrorMessage(string errorMessage, Guid messageId = default) : base(MessageType.RemoteFileError, messageId)
        {
            ErrorMessage = errorMessage;
        }

        public FileErrorMessage(byte[] data) : base(data)
        {
            using (MemoryStream ms = new MemoryStream(data, 21, data.Length - 21))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    int msgLen = br.ReadInt32();
                    ErrorMessage = Encoding.UTF8.GetString(br.ReadBytes(msgLen));
                }
            }
        }

        public override byte[] ToBytes()
        {
            int msgLen = Encoding.UTF8.GetByteCount(ErrorMessage);
            byte[] data = WritePacketInfo(this, 4 + msgLen);
            Buffer.BlockCopy(BitConverter.GetBytes(msgLen), 0, data, 21, 4);
            Buffer.BlockCopy(Encoding.UTF8.GetBytes(ErrorMessage), 0, data, 21 + 4, msgLen);
            return data;
        }
        public string ErrorMessage { get; }
    }
}
