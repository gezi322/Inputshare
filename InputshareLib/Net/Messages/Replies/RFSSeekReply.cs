using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages.Replies
{
    [Serializable]
    internal class RFSSeekReply : NetReplyBase
    {
        public RFSSeekReply(Guid messageId, long position) : base(messageId)
        {
            Position = position;
        }

        public long Position { get; }
    }
}
