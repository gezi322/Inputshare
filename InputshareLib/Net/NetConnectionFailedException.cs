using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net
{
    internal class NetConnectionFailedException : Exception
    {
        internal NetConnectionFailedException(string message) : base(message)
        {

        }
    }
}
