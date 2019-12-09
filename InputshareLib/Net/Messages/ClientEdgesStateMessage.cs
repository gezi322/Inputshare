using System;

namespace InputshareLib.Net.Messages
{
    internal class ClientEdgesStateMessage : NetworkMessage
    {
        public ClientEdgesStateMessage(bool clientTop, bool clientBottom,
            bool clientLeft, bool clientRight,
            Guid messageId = default) : base(MessageType.ClientEdgeStates, messageId)
        {
            ClientTop = clientTop;
            ClientBottom = clientBottom;
            ClientLeft = clientLeft;
            ClientRight = clientRight;
        }

        public ClientEdgesStateMessage(byte[] data) : base(data)
        {
            ClientTop = data[21] != 0;
            ClientBottom = data[22] != 0;
            ClientLeft = data[23] != 0;
            ClientRight = data[24] != 0;

        }

        public override byte[] ToBytes()
        {
            byte[] data = WritePacketInfo(this, 4);
            data[21] = Convert.ToByte(ClientTop);
            data[22] = Convert.ToByte(ClientBottom);
            data[23] = Convert.ToByte(ClientLeft);
            data[24] = Convert.ToByte(ClientRight);

            return data;
        }

        public bool ClientTop { get; }
        public bool ClientBottom { get; }
        public bool ClientLeft { get; }
        public bool ClientRight { get; }
    }
}
