using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages.Replies
{
    [Serializable]
    internal class RFSTokenReply : NetReplyBase
    {
        public RFSTokenReply(Guid tokenId, Guid messageId) : base(messageId)
        {
            TokenId = tokenId;
        }

        public Guid TokenId { get; }
    }
}
