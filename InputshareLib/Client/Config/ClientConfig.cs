using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Inputshare.Common.Client.Config
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

        internal static bool TrySaveLastAddress(IPEndPoint address)
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
