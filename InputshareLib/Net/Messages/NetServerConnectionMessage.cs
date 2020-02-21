using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages
{
    [Serializable]
    internal class NetServerConnectionMessage : NetMessageBase
    {
        public NetServerConnectionMessage(string reply) : base()
        {
            Reply = reply;
        }

        public string Reply { get; }
    }
}
