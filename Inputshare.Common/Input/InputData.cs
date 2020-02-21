using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Input
{
    /// <summary>
    /// Represents a platform full independant mouse or keyboard input
    /// </summary>
    public struct InputData
    {
        public InputCode Code;
        public short ParamA;
        public short ParamB;

        public InputData(InputCode code, short paramA, short paramB)
        {
            Code = code;
            ParamA = paramA;
            ParamB = paramB;
        }

        public byte[] ToBytes()
        {
            byte[] data = new byte[5];
            data[0] = (byte)Code;
            Buffer.BlockCopy(BitConverter.GetBytes(ParamA), 0, data, 1, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(ParamB), 0, data, 3, 2);
            return data;
        }

        public void CopyToBuffer(byte[] buffer, int index)
        {
            buffer[index] = (byte)Code;
            Buffer.BlockCopy(BitConverter.GetBytes(ParamA), 0, buffer, index+1, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(ParamB), 0, buffer, index+3, 2);
        }

        public InputData(byte[] data, int offset)
        {
            Code = (InputCode)data[offset];
            ParamA = BitConverter.ToInt16(data, offset + 1);
            ParamB = BitConverter.ToInt16(data, offset + 3);
        }
    }
}
