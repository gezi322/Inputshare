using Inputshare.Common.Input.Hotkeys;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common
{
    public static class Extensions
    {
        public static Side Opposite(this Side side)
        {
            switch (side)
            {
                case Side.Top:
                    return Side.Bottom;
                case Side.Left:
                    return Side.Right;
                case Side.Bottom:
                    return Side.Top;
                case Side.Right:
                    return Side.Left;
            }

            return 0;
        }

        
        public static IEnumerable<Side> AllSides { get
            {
                yield return Side.Bottom;
                yield return Side.Top;
                yield return Side.Left;
                yield return Side.Right;
            } }

        public static Guid ParseGuid(this byte[] data, int startPos)
        {
            byte[] raw = new byte[16];
            Buffer.BlockCopy(data, startPos, raw, 0, 16);
            return new Guid(raw);
        }

        public static void InsertGuid(this byte[] data, Guid guid, int index)
        {
            Buffer.BlockCopy(guid.ToByteArray(), 0, data, index, 16);
        }

        public static void InsertInt(this byte[] data, int insert, int index)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(insert), 0, data, index, 4);
        }

        public static int ParseInt(this byte[] data, int index)
        {
            return BitConverter.ToInt32(data, index);
        }

        public static string ToHex(this byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }

    }
}
