using System;

namespace InputshareLib.Net.Messages
{
    internal class ClientInitialMessage : NetworkMessage
    {
        public ClientInitialMessage(string clientName, Guid clientId, byte[] displayConfig, string version, int udpPort, Guid messageId = default) : base(MessageType.ClientInitialInfo, messageId)
        {
            if (clientName == null)
                throw new NullReferenceException("clientName");

            if (displayConfig == null)
                throw new NullReferenceException("displayConfig");

            ClientName = clientName;
            ClientId = clientId;
            DisplayConfig = displayConfig;
            Version = version;
            UdpPort = udpPort;
        }

        public ClientInitialMessage(byte[] data) : base(data)
        {
            int nLen = BitConverter.ToInt32(data, 21);
            byte[] nameData = new byte[nLen];
            Buffer.BlockCopy(data, 25, nameData, 0, nLen);
            byte[] idData = new byte[16];
            Buffer.BlockCopy(data, 25 + nLen, idData, 0, 16);
            ClientName = Settings.NetworkMessageTextEncoder.GetString(nameData);
            ClientId = new Guid(idData);
            int dConfLen = BitConverter.ToInt32(data, 25 + nLen + 16);
            DisplayConfig = new byte[dConfLen];
            Buffer.BlockCopy(data, 25 + nLen + 20, DisplayConfig, 0, dConfLen);
            int vLen = BitConverter.ToInt32(data, 45 + nLen + dConfLen);
            byte[] lenBuff = new byte[vLen];
            Buffer.BlockCopy(data, 49 + nLen + dConfLen, lenBuff, 0, vLen);
            Version = Settings.NetworkMessageTextEncoder.GetString(lenBuff);
            UdpPort = BitConverter.ToInt32(data, 49 + nLen + dConfLen + vLen);
        }

        public override byte[] ToBytes()
        {
            byte[] nameData = Settings.NetworkMessageTextEncoder.GetBytes(ClientName);
            byte[] idData = ClientId.ToByteArray();
            byte[] verBuff = Settings.NetworkMessageTextEncoder.GetBytes(Version);
            byte[] data = WritePacketInfo(this, nameData.Length + idData.Length + DisplayConfig.Length + verBuff.Length + 16);
            Buffer.BlockCopy(BitConverter.GetBytes(nameData.Length), 0, data, 21, 4);
            Buffer.BlockCopy(nameData, 0, data, 25, nameData.Length);
            Buffer.BlockCopy(idData, 0, data, 25 + nameData.Length, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(DisplayConfig.Length), 0, data, 25 + nameData.Length + 16, 4);
            Buffer.BlockCopy(DisplayConfig, 0, data, 25 + nameData.Length + 20, DisplayConfig.Length);


            Buffer.BlockCopy(BitConverter.GetBytes(verBuff.Length), 0, data, 45 + nameData.Length + DisplayConfig.Length, 4);
            Buffer.BlockCopy(verBuff, 0, data, 49 + nameData.Length + DisplayConfig.Length, verBuff.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(UdpPort), 0, data, data.Length - 4, 4);
            return data;
        }

        public string ClientName { get; }
        public Guid ClientId { get; }
        public byte[] DisplayConfig { get; }
        public string Version { get; }
        public int UdpPort { get; }
    }
}
