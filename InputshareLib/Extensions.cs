using InputshareLib.Clipboard.DataTypes;
using System;
using System.IO;

namespace InputshareLib
{
    public static class Extensions
    {
        public static bool IsBitSet(this byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }
        public static Edge Opposite(this Edge edge)
        {
            return edge switch
            {
                Edge.Bottom => Edge.Top,
                Edge.Top => Edge.Bottom,
                Edge.Left => Edge.Right,
                Edge.Right => Edge.Left,
                _ => Edge.Top,
            };
        }
        public static Guid ParseGuid(this byte[] data, int startPos)
        {
            if (startPos == 0)
                return new Guid(data);

            byte[] raw = new byte[16];
            Buffer.BlockCopy(data, startPos, raw, 0, 16);
            return new Guid(raw);
        }

        public static void InsertGuid(this byte[] data, Guid guid, int index)
        {
            Buffer.BlockCopy(guid.ToByteArray(), 0, data, index, 16);
        }

        public static void InsertInt(this byte[] data, int index, int insert)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(insert), 0, data, index, 4);
        }

        public static int ParseInt(this byte[] data, int index)
        {
            return BitConverter.ToInt32(data, index);
        }

    }
}
