using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Client
{
    public struct SideStates
    {
        public SideStates(bool left, bool right, bool top, bool bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public bool IsDisplayAtSide(Side side)
        {
            switch (side)
            {
                case Side.Top:
                    return Top;
                case Side.Bottom:
                    return Bottom;
                case Side.Left:
                    return Left;
                case Side.Right:
                    return Right;
                default:
                    return false;
            }
        }

        public bool Left { get; }
        public bool Right { get; }
        public bool Top { get; }
        public bool Bottom { get; }
    }
}
