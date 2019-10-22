using InputshareLib.Displays;
using System;
using System.Collections.Generic;
using System.Text;
using static InputshareLib.Displays.DisplayManagerBase;

namespace InputshareLibWindows.IPC.AnonIpc.Messages
{
    public class AnonIpcDisplayConfigMessage : IpcMessage
    {
        public DisplayConfig Config { get; }

        public AnonIpcDisplayConfigMessage(byte[] data) : base(data)
        {
            byte[] rawConf = new byte[data.Length - 17];
            Buffer.BlockCopy(data, 17, rawConf, 0, rawConf.Length);
            Config = new DisplayConfig(rawConf);
        }

        public override byte[] ToBytes()
        {
            byte[] conf = Config.ToBytes();
            byte[] data = CreateArray(conf.Length);
            Buffer.BlockCopy(conf, 0, data, 17, conf.Length);
            return data;
        }

        public AnonIpcDisplayConfigMessage(DisplayConfig config, Guid messageId = default) : base(IpcMessageType.AnonIpcDisplayConfigReply, messageId)
        {
            Config = config;
        }
    }
}
