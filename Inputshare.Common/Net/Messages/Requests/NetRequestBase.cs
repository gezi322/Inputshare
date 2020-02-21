using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages.Requests
{
    [Serializable]
    internal abstract class NetRequestBase : NetMessageBase
    {
        internal Guid MessageId { get;  set; }
        public NetRequestBase() : base()
        {
            MessageId = Guid.NewGuid();
        }
    }
}
