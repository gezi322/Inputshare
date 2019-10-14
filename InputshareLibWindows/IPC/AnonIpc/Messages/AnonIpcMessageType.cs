using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.AnonIpc.Messages
{
    public enum AnonIpcMessageType
    {
        HostOK = 1,
        ClientOK = 2,
        InputData = 3,
        DisplayConfigRequest = 4,
        DisplayConfigReply = 5,
        LMouseStateRequest = 6,
        LMouseStateReply = 7,
        EdgeHit = 8,
        DoDragDrop = 9,
        DragDropSuccess = 10,
        DragDropCancelled = 11,
        DragDropComplete = 12,
        CheckForDrop = 13,
        StreamReadRequest = 14,
        StreamReadResponse = 15,
        StreamReadError = 16,
        ClipboardData = 17,

    }
}
