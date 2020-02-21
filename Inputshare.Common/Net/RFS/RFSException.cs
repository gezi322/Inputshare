using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.RFS
{
    [Serializable]
    internal class RFSException : Exception
    {
        internal RFSException(string message) : base(message)
        {

        }
    }
}
