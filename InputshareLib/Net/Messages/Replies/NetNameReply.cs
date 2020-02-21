using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages.Replies
{
    [Serializable]
    internal class NetNameReply : NetReplyBase
    {
        public NetNameReply(string name, Guid messageId) : base(messageId)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
