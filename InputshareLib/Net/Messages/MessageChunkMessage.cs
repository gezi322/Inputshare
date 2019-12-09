using System;

namespace InputshareLib.Net.Messages
{
    internal class MessageChunkMessage : NetworkMessage
    {
        public byte[] MessageData { get; }
        public int MessageSize { get; }

        public override byte[] ToBytes()
        {
            byte[] data = WritePacketInfo(this, MessageData.Length + 8);
            Buffer.BlockCopy(BitConverter.GetBytes(MessageSize), 0, data, 21, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(MessageData.Length), 0, data, 25, 4);
            Buffer.BlockCopy(MessageData, 0, data, 29, MessageData.Length);
            return data;
        }

        public MessageChunkMessage(Guid messageId, byte[] data, int messageSize) : base(MessageType.MessagePart, messageId)
        {
            MessageData = data;
            MessageSize = messageSize;
        }

        public MessageChunkMessage(byte[] data) : base(data)
        {
            MessageSize = BitConverter.ToInt32(data, 21);
            int pSize = BitConverter.ToInt32(data, 25);
            MessageData = new byte[pSize];
            Buffer.BlockCopy(data, 29, MessageData, 0, pSize);
        }

    }
}
