using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text;

namespace InputshareLib.Net.Client
{
    internal class ClientConnectArgs
    {
        internal ClientConnectArgs(IPEndPoint address, string name, Guid id, Rectangle virtualBounds)
        {
            Address = address;
            Name = name;
            Id = id;
            VirtualBounds = virtualBounds;
        }

        public IPEndPoint Address { get; }
        public string Name { get; }
        public Guid Id { get; }
        public Rectangle VirtualBounds { get; }
    }
}
