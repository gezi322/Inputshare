using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages
{
    [Serializable]
    internal class NetMessageSegment : NetMessageBase
    {
        public NetMessageSegment(Guid messageId, byte[] data, int fullPacketSize)
        {
            MessageId = messageId;
            Data = data;
            FullPacketSize = fullPacketSize;
        }

        public Guid MessageId { get; }
        public byte[] Data { get; }
        public int FullPacketSize { get; }
    }
}
