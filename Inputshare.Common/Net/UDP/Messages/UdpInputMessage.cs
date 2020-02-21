using Inputshare.Common.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.UDP.Messages
{
    internal class UdpInputMessage : IUdpMessage
    {
        public UdpMessageType Type => UdpMessageType.InputData;
        internal readonly InputData Input;

        internal UdpInputMessage(InputData input)
        {
            Input = input;
        }

        public byte[] ToBytes()
        {
            byte[] data = new byte[6];
            data[0] = (byte)Type;
            Input.CopyToBuffer(data, 1);
            return data;
        }
    }
}
