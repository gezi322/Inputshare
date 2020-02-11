using System;
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
    }
}
