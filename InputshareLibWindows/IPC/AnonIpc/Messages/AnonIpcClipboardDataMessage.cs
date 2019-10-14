using InputshareLib.Clipboard.DataTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.AnonIpc.Messages
{
    public class AnonIpcClipboardDataMessage : AnonIpcMessage
    {
        public AnonIpcClipboardDataMessage(byte[] data) : base(data)
        {
            byte[] cbData = new byte[data.Length - 17];
            Buffer.BlockCopy(data, 17, cbData, 0, cbData.Length);
            Data = ClipboardDataBase.FromBytes(cbData);
        }

        public override byte[] ToBytes()
        {
            byte[] cbData = Data.ToBytes();
            byte[] data = CreateArray(cbData.Length);
            Buffer.BlockCopy(cbData, 0, data, 17, cbData.Length);
            return data;
        }

        public AnonIpcClipboardDataMessage(ClipboardDataBase data, Guid messageId = default) : base(AnonIpcMessageType.ClipboardData, messageId)
        {
            Data = data;
        }

        public ClipboardDataBase Data { get; }
    }
}
