using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages.Requests
{
    [Serializable]
    internal abstract class NetRequestBase : NetMessageBase
    {
        internal Guid MessageId { get; }
        public NetRequestBase() : base()
        {
            MessageId = Guid.NewGuid();
        }
    }
}
