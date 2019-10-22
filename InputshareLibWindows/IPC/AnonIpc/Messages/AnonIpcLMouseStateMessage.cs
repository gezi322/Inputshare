using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.AnonIpc.Messages
{
    public class AnonIpcLMouseStateMessage : IpcMessage
    {
        public bool LeftMouseState { get; }

        public AnonIpcLMouseStateMessage(byte[] data) : base(data)
        {
            LeftMouseState = Convert.ToBoolean(data[17]);
        }

        public override byte[] ToBytes()
        {
            byte[] data = CreateArray(1);
            data[17] = Convert.ToByte(LeftMouseState);
            return data;
        }

        public AnonIpcLMouseStateMessage(bool state, Guid messageId = default) : base(IpcMessageType.AnonIpcLMouseStateReply, messageId)
        {
            LeftMouseState = state;
        }
    }
}
