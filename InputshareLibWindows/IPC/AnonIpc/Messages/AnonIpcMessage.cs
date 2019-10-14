using InputshareLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.AnonIpc.Messages
{
    public class AnonIpcMessage
    {
        public AnonIpcMessageType MessageType { get; }
        public Guid MessageId { get; }

        public AnonIpcMessage(AnonIpcMessageType type, Guid messageId = default)
        {
            MessageType = type;

            if (messageId == default)
                MessageId = Guid.NewGuid();
            else
                MessageId = messageId;
        }

        public virtual byte[] ToBytes()
        {
            return CreateArray(0);
        }

        protected byte[] CreateArray(int size)
        {
            byte[] data = new byte[17+size];
            data[0] = (byte)MessageType;
            data.InsertGuid(MessageId, 1);
            return data;
        }

        public AnonIpcMessage(byte[] data)
        {
            MessageType = (AnonIpcMessageType)data[0];
            MessageId = data.ParseGuid(1);
        }
    }
}
