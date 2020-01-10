using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Input.Hotkeys;

namespace InputshareLib.Server
{
    internal static class ClientConfig
    {
        internal static void SaveClientEdge(ISServerSocket client, Edge edge)
        {
            if(edge == Edge.Bottom)
                ConfigFile.TryWrite(client.ClientName + "-bottom", client.BottomClient == null ? "None" : client.BottomClient.ClientName);
            else if(edge == Edge.Top)
                ConfigFile.TryWrite(client.ClientName + "-top", client.TopClient == null ? "None" : client.TopClient.ClientName);
            else if(edge == Edge.Right)
                ConfigFile.TryWrite(client.ClientName + "-right", client.RightClient == null ? "None" : client.RightClient.ClientName);
            else if(edge == Edge.Left)
                ConfigFile.TryWrite(client.ClientName + "-left", client.LeftClient == null ? "None" : client.LeftClient.ClientName);
        }

        internal static void SaveClientHotkey(ISServerSocket client, Hotkey hk)
        {
            ConfigFile.TryWrite(client.ClientName + "-hotkey", hk == null ? "None" : hk.ToSettingsString());
        }

        internal static void ReloadClientConfigs(ClientManager clientMan)
        {
            foreach(var client in clientMan.AllClients)
            {
                if (ConfigFile.TryRead(client + "-left", out string target))
                    SetEdgeIfExists(client, Edge.Left, target, clientMan);
                if (ConfigFile.TryRead(client + "-right", out target))
                    SetEdgeIfExists(client, Edge.Right, target, clientMan);
                if (ConfigFile.TryRead(client + "-top", out target))
                    SetEdgeIfExists(client, Edge.Top, target, clientMan);
                if (ConfigFile.TryRead(client + "-bottom", out target))
                    SetEdgeIfExists(client, Edge.Bottom, target, clientMan);

                if (ConfigFile.TryRead(client + "-hotkey", out string hkStr))
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
            client.SetClientAtEdge(edge, target);
        }
    }
}
