using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Inputshare.Common.Client.Config
{
    internal static class ClientConfig
    {
        internal static bool BroadcastEnabled = true;
        internal static int BroadcastPort = 8888;
        internal static IPEndPoint LastAddress = new IPEndPoint(IPAddress.Any, 0);
        internal static string ClientName = Environment.MachineName;
        internal static bool HideCursor = true;
        internal static bool BindUDP = true;
        internal static int SpecifyUDPPort;

        internal static void LoadConfig()
        {
            if (DllConfig.TryReadProperty("Client.LastAddress", out var result))
                IPEndPoint.TryParse(result, out LastAddress);
            if (DllConfig.TryReadProperty("Client.Name", out result))
                ClientName = result;
            if (DllConfig.TryReadProperty("Client.Broadcast.Enabled", out result))
                bool.TryParse(result, out BroadcastEnabled);
            if (DllConfig.TryReadProperty("Client.Broadcast.Port", out result))
                int.TryParse(result, out BroadcastPort);
            if (DllConfig.TryReadProperty("Client.HideCursor", out result))
                bool.TryParse(result, out HideCursor);
            if (DllConfig.TryReadProperty("Client.BindUDP", out result))
                bool.TryParse(result, out BindUDP);
            if (DllConfig.TryReadProperty("Client.SpecifyUDPPort", out result))
                int.TryParse(result, out SpecifyUDPPort);

        }

        internal static bool TrySaveLastAddress(IPEndPoint address)
        {
            return DllConfig.TryWrite("Client.LastAddress", address.ToString());
        }

        internal static bool TrySaveLastClientName(string clientName)
        {
            return DllConfig.TryWrite("Client.Name", clientName);
        }
    }
}
