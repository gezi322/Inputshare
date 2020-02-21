using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net
{
    internal class NetConnectionFailedException : Exception
    {
        internal NetConnectionFailedException(string message) : base(message)
        {

        }
    }
}
