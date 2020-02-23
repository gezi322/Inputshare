using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Server.Config
{
    internal static class ServerConfig
    {
        internal static bool HideCursor = true;
        internal static bool BindUDP = true;
        internal static bool BroadcastEnabled = true;
        internal static int BroadcastPort = 8888;

        /// <summary>
        /// Loads config values
        /// </summary>
        internal static void LoadConfig()
        {
            if (DllConfig.TryReadProperty("Server.HideCursor", out string result))
                bool.TryParse(result, out HideCursor);

            if (DllConfig.TryReadProperty("Server.BindUDP", out result))
                bool.TryParse(result, out BindUDP);

            if (DllConfig.TryReadProperty("Server.Broadcast.Enabled", out result))
                bool.TryParse(result, out BroadcastEnabled);

            if (DllConfig.TryReadProperty("Server.Broadcast.Port", out result))
                int.TryParse(result, out BroadcastPort);
        }
    }
}
