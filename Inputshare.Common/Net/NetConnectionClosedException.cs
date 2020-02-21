using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net
{
    internal class NetConnectionClosedException : Exception
    {
        internal NetConnectionClosedException() : base("The socket was closed")
        {

        }
    }
}
