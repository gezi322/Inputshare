using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Server.API
{
    public class CurrentClipboardData
    {
        public CurrentClipboardData(ClipboardDataType type, ClientInfo host, DateTime copyTime)
        {
            Type = type;
            Host = host;
            CopyTime = copyTime;
        }

        public ClipboardDataType Type { get; }
        public ClientInfo Host { get; }
        public DateTime CopyTime { get; }

        public enum ClipboardDataType
        {
            None,
            Text,
            Image,
            File
        }
    }
}
