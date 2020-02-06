using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages.Requests
{
    [Serializable]
    internal abstract class NetRequestBase : NetMessageBase
    {
        public NetRequestBase() : base()
        {
        }
    }
}
