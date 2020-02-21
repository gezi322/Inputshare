using Inputshare.Common.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages
{
    /// <summary>
    /// Contains information about a network message. Contains an InputData struct
    /// if the MessageType is InputMessage
    /// </summary>
    internal struct NetMessageHeader
    {
        /// <summary>
        /// The size of a network message header
        /// </summary>
        internal const int HeaderSize = 6;

        internal NetMessageType MessageType => (NetMessageType)Data[0];
        internal int MessageLength => BitConverter.ToInt32(Data, 1);
        internal Input.InputData Input => new Input.InputData(Data, 1);

        internal byte[] Data;

        internal static NetMessageHeader CreateStandardHeader(int messageLen)
        {
            NetMessageHeader header;
            header.Data = new byte[6];
            header.Data[0] = (byte)NetMessageType.StandardMessage;
            Buffer.BlockCopy(BitConverter.GetBytes(messageLen), 0, header.Data, 1, 4);
            return header;
        }

        internal static NetMessageHeader CreateInputHeader(ref InputData input)
        {
            NetMessageHeader header;
            header.Data = new byte[6];
            header.Data[0] = (byte)NetMessageType.InputData;
            input.CopyToBuffer(header.Data, 1);
            return header;
        }

        internal static NetMessageHeader CreateCustomSerializedHeader(int messageLen, NetMessageType type)
        {
            NetMessageHeader header;
            header.Data = new byte[6];
            header.Data[0] = (byte)type;
            Buffer.BlockCopy(BitConverter.GetBytes(messageLen), 0, header.Data, 1, 4);
            return header;
        }

        internal static NetMessageHeader ReadFromBuffer(byte[] buffer, int offset)
        {
            NetMessageHeader header;
            header.Data = new byte[6];
            Buffer.BlockCopy(buffer, offset, header.Data, 0, 6);
            return header;
        }
    }
}
