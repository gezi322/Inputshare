using System;

namespace InputshareLib.Net.Messages
{
    internal class ClientDeclinedMessage : NetworkMessage
    {
        public string Reason { get; }

        public ClientDeclinedMessage(string reason, Guid messageId = default) : base(MessageType.ClientDeclined, messageId)
        {
            Reason = reason ?? throw new ArgumentNullException("reason");
        }

        public ClientDeclinedMessage(byte[] data) : base(data)
        {
            int len = BitConverter.ToInt32(data, 21);
            Reason = Settings.NetworkMessageTextEncoder.GetString(data, 25, len);
        }

        public override byte[] ToBytes()
        {
            int rLen = Settings.NetworkMessageTextEncoder.GetByteCount(Reason);
            byte[] data = WritePacketInfo(this, rLen + 4);
            Buffer.BlockCopy(BitConverter.GetBytes(rLen), 0, data, 21, 4);
            Buffer.BlockCopy(Settings.NetworkMessageTextEncoder.GetBytes(Reason), 0, data, 25, rLen);
            return data;
        }
    }
}
