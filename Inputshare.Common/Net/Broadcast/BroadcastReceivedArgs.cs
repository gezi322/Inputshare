using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace Inputshare.Common.Net.Broadcast
{
    public class BroadcastReceivedArgs : EventArgs
    {
        internal BroadcastReceivedArgs(IPEndPoint server, PingReply pingReply, string serverVersion)
        {
            Server = server;
            PingReply = pingReply;
            ServerVersion = serverVersion;
        }

        public IPEndPoint Server { get; }
        public PingReply PingReply { get; }
        public string ServerVersion { get; }
    }
}
