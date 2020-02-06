using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net
{
    internal class NetRequestTimedOutException : Exception
    {
        internal NetRequestTimedOutException() : base("The network request did not receive a reply")
        {

        }
    }
}
