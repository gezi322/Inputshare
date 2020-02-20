using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net
{
    internal class NetConnectionClosedException : Exception
    {
        internal NetConnectionClosedException() : base("The socket was closed")
        {

        }
    }
}
