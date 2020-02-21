﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Inputshare.Common.Net.Messages
{
    [Serializable]
    internal class NetDisplayBoundsUpdateMessage : NetMessageBase
    {
        internal NetDisplayBoundsUpdateMessage(Rectangle newBounds)
        {
            NewBounds = newBounds;
        }

        public Rectangle NewBounds { get; }
    }
}
