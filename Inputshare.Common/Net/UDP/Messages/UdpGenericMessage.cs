using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.UDP.Messages
{
    internal class UdpGenericMessage : IUdpMessage
    {
        public UdpMessageType Type { get; }

        internal UdpGenericMessage(UdpMessageType message)
        {
            Type = message;
        }

       public byte[] ToBytes()
        {
            return new byte[]
            {
                (byte)Type
            };
        }
    }
}
