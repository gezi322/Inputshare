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
                Config.TryWrite(client.ClientName + "-bottom", client.BottomClient == null ? "None" : client.BottomClient.ClientName);
            else if(edge == Edge.Top)
                Config.TryWrite(client.ClientName + "-top", client.TopClient == null ? "None" : client.TopClient.ClientName);
            else if(edge == Edge.Right)
                Config.TryWrite(client.ClientName + "-right", client.RightClient == null ? "None" : client.RightClient.ClientName);
            else if(edge == Edge.Left)
                Config.TryWrite(client.ClientName + "-left", client.LeftClient == null ? "None" : client.LeftClient.ClientName);
        }

        internal static void SaveClientHotkey(ISServerSocket client, Hotkey hk)
        {
            Config.TryWrite(client.ClientName + "-hotkey", hk == null ? "None" : hk.ToSettingsString());
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
