using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Input.Hotkeys;

namespace InputshareLib.Server
{
    internal static class ClientConfig
    {
        internal static void SaveAllClientConfigs(ClientManager clientMan)
        {
            foreach(var client in clientMan.AllClients)
            {
                Config.TryWrite(client.ClientName + "-left", client.LeftClient == null ? "None" : client.LeftClient.ClientName);
                Config.TryWrite(client.ClientName + "-right", client.RightClient == null ? "None" : client.RightClient.ClientName);
                Config.TryWrite(client.ClientName + "-top", client.TopClient == null ? "None" : client.TopClient.ClientName);
                Config.TryWrite(client.ClientName + "-bottom", client.BottomClient == null ? "None" : client.BottomClient.ClientName);

                Config.TryWrite(client.ClientName + "-hotkey", client.CurrentHotkey == null ? "None" : client.CurrentHotkey.ToSettingsString());
            }

            
        }

        internal static void ReloadClientConfigs(ClientManager clientMan)
        {
            foreach(var client in clientMan.AllClients)
            {
                if (Config.TryRead(client + "-left", out string target))
                    SetEdgeIfExists(client, Edge.Left, target, clientMan);
                if (Config.TryRead(client + "-right", out target))
                    SetEdgeIfExists(client, Edge.Right, target, clientMan);
                if (Config.TryRead(client + "-top", out target))
                    SetEdgeIfExists(client, Edge.Top, target, clientMan);
                if (Config.TryRead(client + "-bottom", out target))
                    SetEdgeIfExists(client, Edge.Bottom, target, clientMan);

                if (Config.TryRead(client + "-hotkey", out string hkStr))
                    if (Hotkey.TryFromSettingsString(hkStr, out Hotkey key))
                        client.CurrentHotkey = key;
            }
        }
        
        private static void SetEdgeIfExists(ISServerSocket client, Edge edge, string targetStr, ClientManager clientMan)
        {
            if (!clientMan.TryGetClientByName(targetStr, out var target))
            {
                return;
            }

            client.SetClientAtEdgeNoUpdate(edge, target);
        }
    }
}
