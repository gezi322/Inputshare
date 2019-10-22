using InputshareLib.Clipboard.DataTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.AnonIpc.Messages
{
    public class AnonIpcDoDragDropMessage : IpcMessage
    {
        public ClipboardDataBase DropData { get; }

        public AnonIpcDoDragDropMessage(byte[] data) : base(data)
        {
            byte[] raw = new byte[data.Length - 17];
            Buffer.BlockCopy(data, 17, raw, 0, raw.Length);
            DropData = ClipboardDataBase.FromBytes(raw);
        }

        public override byte[] ToBytes()
        {
            byte[] raw = DropData.ToBytes();
            byte[] data = CreateArray(raw.Length);
            Buffer.BlockCopy(raw, 0, data, 17, raw.Length);
            return data;
        }

        public AnonIpcDoDragDropMessage(ClipboardDataBase dragData, Guid messageId = default) : base(IpcMessageType.AnonIpcDoDragDrop, messageId)
        {
            DropData = dragData;
        }
    }
}
