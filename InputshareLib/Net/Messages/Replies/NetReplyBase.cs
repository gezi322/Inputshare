using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages.Replies
{
    [Serializable]
    internal abstract class NetReplyBase : NetMessageBase
    {
        internal NetReplyBase(Guid messageId) : base(messageId)
        {
        }
    }
}
