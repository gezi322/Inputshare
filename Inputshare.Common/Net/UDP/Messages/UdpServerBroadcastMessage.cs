using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.UDP.Messages
{
    internal class UdpServerBroadcastMessage : IUdpMessage
    {
        public UdpMessageType Type => UdpMessageType.ServerBroadcast;
        public string Address { get; private set; }

        public UdpServerBroadcastMessage(string address)
        {
            Address = address;
        }

        public byte[] ToBytes()
        {
            int len = Encoding.Unicode.GetMaxByteCount(Address.Length);
            byte[] data = new byte[len + 5];
            data[0] = (byte)Type;
            Buffer.BlockCopy(BitConverter.GetBytes(len), 0, data, 1, 4);
            byte[] txt = Encoding.Unicode.GetBytes(Address);
            Buffer.BlockCopy(txt, 0, data, 5, txt.Length);
            return data;
        }
    }
}
