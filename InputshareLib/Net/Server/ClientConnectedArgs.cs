using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace InputshareLib.Net.Server
{
    internal class ClientConnectedArgs
    {
        internal ClientConnectedArgs(ServerSocket socket, string name, Guid id, Rectangle displayBounds)
        {
            Socket = socket;
            Name = name;
            Id = id;
            DisplayBounds = displayBounds;
        }

        public ServerSocket Socket { get; }
        public string Name { get; }
        public Guid Id { get; }
        public Rectangle DisplayBounds { get; }
    }
}
