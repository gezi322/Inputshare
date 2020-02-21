using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages.Replies
{
    [Serializable]
    internal abstract class NetReplyBase : NetMessageBase
    {
        internal NetReplyBase(Guid messageId) : base()
        {
            MessageId = messageId;
        }

        public Guid MessageId { get; internal set; }
    }
}
