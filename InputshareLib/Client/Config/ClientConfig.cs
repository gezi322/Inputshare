using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace InputshareLib.Client.Config
{
    internal static class ClientConfig
    {
        internal static bool TryGetLastAddress(out IPEndPoint address)
        {
            if(DllConfig.TryReadProperty("Client.LastAddress", out var addressString)){
                if (IPEndPoint.TryParse(addressString, out address))
                    return true;
                else
                    return false;
            }
            else
            {
                address = null;
                return false;
            }
        }

        internal static bool trySaveLastAddress(IPEndPoint address)
        {
            return DllConfig.TryWrite("Client.LastAddress", address.ToString());
        }

        internal static bool TrySaveLastClientName(string clientName)
        {
            return DllConfig.TryWrite("Client.Name", clientName);
        }

        internal static bool TryGetLastClientName(out string clientName)
        {
            return DllConfig.TryReadProperty("Client.Name", out clientName);
        }
    }
}
