using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages.Replies
{
    [Serializable]
    internal class NetScreenshotReply : NetReplyBase
    {
        public NetScreenshotReply(Guid messageId, byte[] bmp) : base(messageId)
        {
            Bmp = bmp;
        }

        public byte[] Bmp { get; }
    }
}
