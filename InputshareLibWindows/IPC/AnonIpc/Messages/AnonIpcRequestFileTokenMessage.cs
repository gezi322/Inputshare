using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib;

namespace InputshareLibWindows.IPC.AnonIpc.Messages
{
    public class AnonIpcRequestFileTokenMessage : IpcMessage
    {
        public Guid OperationId { get; }

        public AnonIpcRequestFileTokenMessage(byte[] data) : base(data)
        {
            OperationId = data.ParseGuid(17);
        }

        public override byte[] ToBytes()
        {
            byte[] data = CreateArray(16);
            data.InsertGuid(OperationId, 17);
            return data;
        }

        public AnonIpcRequestFileTokenMessage(Guid operationId, Guid messageId = default) : base(IpcMessageType.AnonIpcRequestFileToken, messageId)
        {
            OperationId = operationId;
        }
    }
}
