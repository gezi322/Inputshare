using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Inputshare.Common.Net.Server
{
    internal class ClientConnectedArgs
    {
        internal ClientConnectedArgs(ServerSocket socket, string name, Guid id, Rectangle displayBounds, int udpPort)
        {
            Socket = socket;
            Name = name;
            Id = id;
            DisplayBounds = displayBounds;
            UdpPort = udpPort;
        }

        public ServerSocket Socket { get; }
        public string Name { get; }
        public Guid Id { get; }
        public Rectangle DisplayBounds { get; }
        public int UdpPort { get; }
    }
}
