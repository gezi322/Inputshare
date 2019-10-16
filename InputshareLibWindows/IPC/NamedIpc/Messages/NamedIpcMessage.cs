using InputshareLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.NamedIpc.Messages
{

    [Serializable]
    public class NamedIpcMessage
    {
        public NamedIpcMessageType MessageType { get; }
        public Guid MessageId { get; }

        public NamedIpcMessage(NamedIpcMessageType type, Guid messageId = default)
        {
            MessageType = type;

            if (messageId == default)
                MessageId = Guid.NewGuid();
            else
                MessageId = messageId;
        }
    }
}
