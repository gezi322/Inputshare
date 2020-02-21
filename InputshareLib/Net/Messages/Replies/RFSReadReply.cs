using Inputshare.Common.Net.RFS;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages.Replies
{
    [Serializable]
    internal class RFSReadReply : NetReplyBase
    {
        public RFSReadReply(Guid messageId, byte[] returnData) : base(messageId)
        {
            ReturnData = returnData;
        }

        public byte[] ReturnData { get; }
        public Exception Ex { get; }
    }
}
