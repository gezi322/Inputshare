using Inputshare.Common.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Inputshare.Common.Net.UDP.Messages
{
    internal static class UdpMessageFactory
    {
        internal static IUdpMessage ReadMessage(byte[] message)
        {
            switch ((UdpMessageType)message[0])
            {
                case UdpMessageType.InputData:
                    return ReadInputMessage(message);
                case UdpMessageType.ServerBroadcast:
                    return ReadServerBroadcast(message);
                default:
                    return new UdpGenericMessage((UdpMessageType)message[0]);
            }
        }
        
        private static UdpServerBroadcastMessage ReadServerBroadcast(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    ms.Position = 1;
                    var address = br.ReadString();
                    var serverVer = br.ReadString();
                    return new UdpServerBroadcastMessage(address, serverVer);
                }
            }
        }


        private static UdpInputMessage ReadInputMessage(byte[] data)
        {
            return new UdpInputMessage(new InputData(data, 1));
        }
    }
}
