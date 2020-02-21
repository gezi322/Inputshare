using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages
{
    internal enum NetMessageType
    {
        StandardMessage,
        InputData,
        
        //Custom serialized message
        CustomSerializedStart = 100,
        RFSReadRequest,
        RFSReadReply,
    }
}
