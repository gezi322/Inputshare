using System;

namespace InputshareLib.Net.Messages
{
    internal class DisplayConfigMessage : NetworkMessage
    {
        public byte[] ConfigData { get; }

        public override byte[] ToBytes()
        {
            byte[] data = WritePacketInfo(this, ConfigData.Length + 4);
            Buffer.BlockCopy(BitConverter.GetBytes(ConfigData.Length), 0, data, 21, 4);
            Buffer.BlockCopy(ConfigData, 0, data, 25, ConfigData.Length);
            return data;
        }

        public DisplayConfigMessage(byte[] configData, Guid messageId = default) : base(MessageType.DisplayConfig, messageId)
        {
            ConfigData = configData;
        }

        public DisplayConfigMessage(byte[] data) : base(data)
        {
            int cLen = BitConverter.ToInt32(data, 21);
            ConfigData = new byte[cLen];
            Buffer.BlockCopy(data, 25, ConfigData, 0, cLen);
        }
    }
}
