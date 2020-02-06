using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace InputshareLib.Net.Messages
{
    [Serializable]
    internal class NetClientConnectionMessage : NetMessageBase
    {
        internal string ClientName { get; }
        public Guid ClientId { get; }
        public string ClientVersion { get; }
        public Rectangle DisplayBounds { get; }

        internal NetClientConnectionMessage(string clientName, Guid clientId, string clientVer, Rectangle displayBounds) : base()
        {
            ClientName = clientName;
            ClientId = clientId;
            ClientVersion = clientVer;
            DisplayBounds = displayBounds;
        }
    }
}
