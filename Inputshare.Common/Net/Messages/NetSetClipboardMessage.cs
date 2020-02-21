using Inputshare.Common.Clipboard;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages
{
    [Serializable]
    internal class NetSetClipboardMessage : NetMessageBase
    {
        public NetSetClipboardMessage(ClipboardData cbData)
        {
            Data = cbData;
        }

        public ClipboardData Data { get; }
    }
}
