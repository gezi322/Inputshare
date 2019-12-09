using System;

namespace InputshareLib.Net.Messages
{
    internal class ClipboardDataMessage : NetworkMessage
    {
        public byte[] cbData { get; }
        public Guid OperationId { get; }

        public override byte[] ToBytes()
        {
            byte[] data = WritePacketInfo(this, cbData.Length + 20);
            Buffer.BlockCopy(OperationId.ToByteArray(), 0, data, 21, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(cbData.Length), 0, data, 21 + 16, 4);
            Buffer.BlockCopy(cbData, 0, data, 25 + 16, cbData.Length);

            return data;
        }

        public ClipboardDataMessage(byte[] data, Guid operationId, Guid messageId = default) : base(MessageType.ClipboardData, messageId)
        {
            cbData = data;
            OperationId = operationId;
        }

        public ClipboardDataMessage(byte[] data) : base(data)
        {
            byte[] idData = new byte[16];
            Buffer.BlockCopy(data, 21, idData, 0, 16);
            OperationId = new Guid(idData);
            int pSize = BitConverter.ToInt32(data, 21 + 16);
            cbData = new byte[pSize];
            Buffer.BlockCopy(data, 25 + 16, cbData, 0, pSize);
        }
    }
}
