using InputshareLib.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages
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

        internal bool IsInput => Convert.ToBoolean(Data[0]);
        internal int MessageLength => BitConverter.ToInt32(Data, 1);
        internal Input.InputData Input => new Input.InputData(Data, 1);

        internal byte[] Data;

        internal NetMessageHeader(int messageLen)
        {
            Data = new byte[6];
            Data[0] = Convert.ToByte(false);
            Buffer.BlockCopy(BitConverter.GetBytes(messageLen), 0, Data, 1, 4);
        }

        internal NetMessageHeader(byte[] buffer, int offset)
        {
            Data = new byte[6];

            Buffer.BlockCopy(buffer, offset, Data, 0, 6);
        }

        /// <summary>
        /// Creates a network message header that contains input data
        /// </summary>
        /// <param name="input"></param>
        internal NetMessageHeader(InputData input)
        {
            Data = new byte[6];
            Data[0] = Convert.ToByte(true);
            input.CopyToBuffer(Data, 1);
        }
    }
}
