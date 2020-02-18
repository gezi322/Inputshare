using InputshareLib.Net.RFS;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages.Replies
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
