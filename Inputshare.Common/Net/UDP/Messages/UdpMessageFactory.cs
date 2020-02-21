using Inputshare.Common.Input;
using System;
using System.Collections.Generic;
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
            int mLen = BitConverter.ToInt32(data, 1);
            return new UdpServerBroadcastMessage(Encoding.Unicode.GetString(data, 5, mLen));
        }


        private static UdpInputMessage ReadInputMessage(byte[] data)
        {
            return new UdpInputMessage(new InputData(data, 1));
        }
    }
}
