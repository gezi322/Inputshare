using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common
{
    public class SideHitArgs : EventArgs
    {
        public SideHitArgs(Side side, int posX, int posY)
        {
            Side = side;
            PosX = posX;
            PosY = posY;
        }

        public Side Side { get; }
        public int PosX { get; }
        public int PosY { get; }
    }
}
