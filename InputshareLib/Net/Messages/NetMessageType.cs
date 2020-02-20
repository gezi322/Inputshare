using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages
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
