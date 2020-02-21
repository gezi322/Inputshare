using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.UDP.Messages
{
    internal interface IUdpMessage
    {
        UdpMessageType Type { get; }
        byte[] ToBytes();
    }
}
