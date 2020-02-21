using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net
{
    internal class NetRequestTimedOutException : Exception
    {
        internal NetRequestTimedOutException() : base("The network request did not receive a reply")
        {

        }
    }
}
