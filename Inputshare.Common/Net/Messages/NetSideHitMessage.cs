using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages
{
    [Serializable]
    internal class NetSideHitMessage : NetMessageBase
    {
        public NetSideHitMessage(Side hitSide, int posX, int posY) : base()
        {
            HitSide = hitSide;
            PosX = posX;
            PosY = posY;
        }

        public Side HitSide { get; }
        public int PosX { get; }
        public int PosY { get; }
    }
}
