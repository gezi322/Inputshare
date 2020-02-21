using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Client
{
    internal class ClientSidesChangedArgs
    {
        internal ClientSidesChangedArgs(bool left, bool right, bool top, bool bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public bool Left { get; }
        public bool Right { get; }
        public bool Top { get; }
        public bool Bottom { get; }
    }
}
