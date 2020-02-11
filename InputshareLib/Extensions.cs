using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib
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
    }
}
