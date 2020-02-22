using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Inputshare.Common.Net.UDP.Messages
{
    internal class UdpServerBroadcastMessage : IUdpMessage
    {
        public UdpMessageType Type => UdpMessageType.ServerBroadcast;
        public string Address { get; private set; }
        public string ServerVersion { get; private set; }

        public UdpServerBroadcastMessage(string address, string version)
        {
            ServerVersion = version;    
            Address = address;
        }

        public byte[] ToBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write((byte)Type);
                    bw.Write(Address);
                    bw.Write(ServerVersion);
                    return ms.ToArray();
                }
            }
        }
    }
}
