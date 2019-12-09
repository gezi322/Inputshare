using InputshareLib.Displays;
using InputshareLib.Input.Hotkeys;
using System;
using System.ComponentModel;
using System.Net;

namespace InputshareLib.Server
{
    /// <summary>
    /// Represents a client connected to the inputshare server (for use with a UI)
    /// </summary>
    public class ClientInfo : INotifyPropertyChanged
    {
        public event EventHandler<string> Disconnected;
        public event PropertyChangedEventHandler PropertyChanged;

        private ISServerSocket host;
        private ClientManager clientMan;

        internal ClientInfo(ISServerSocket client, ClientManager clientManager)
        {
            clientMan = clientManager;
            host = client;

            if (host == null)
                host = ISServerSocket.Localhost;

            host.ConnectionError += Disconnected;
            host.ClientEdgeUpdated += Host_ClientEdgeUpdated;
        }

        private void Host_ClientEdgeUpdated(object sender, Edge e)
        {
            switch (e) {
                case Edge.Bottom:
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BottomClient)));
                    return;
                case Edge.Left:
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LeftClient)));
                    return;
                case Edge.Right:
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RightClient)));
                    return;
                case Edge.Top:
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopClient)));
                    return;
            }
        }

        public override bool Equals(object obj)
        {
            if(obj is ClientInfo b)
            {
                return b.Id == Id;
            }

            return false;
        }

        public override string ToString()
        {
            return Name;
        }

        public ClientInfo LeftClient { get { return host.LeftClient == null ? ClientInfo.None : new ClientInfo(host.LeftClient, clientMan); } }
        public ClientInfo RightClient { get { return host.RightClient == null ? ClientInfo.None : new ClientInfo(host.RightClient, clientMan); } }
        public ClientInfo TopClient { get { return host.TopClient == null ? ClientInfo.None : new ClientInfo(host.TopClient, clientMan); } }
        public ClientInfo BottomClient { get { return host.BottomClient == null ? ClientInfo.None : new ClientInfo(host.BottomClient, clientMan); } }
        public bool InputClient { get { return false; } }
        public string Name { get { return host.ClientName; } }
        public Guid Id { get { return host.ClientId; } }
        public DisplayConfig DisplayConf { get { return host.DisplayConfiguration == null ? new DisplayConfig(new System.Drawing.Rectangle(0, 0, 0, 0), new System.Collections.Generic.List<Display>()) : host.DisplayConfiguration; } }
        public Hotkey ClientHotkey { get { return host.CurrentHotkey == null ? new Hotkey(0, 0) : host.CurrentHotkey; } } 
        public IPEndPoint ClientAddress { get { return host.ClientEndpoint; } }
        public bool UdpEnabled { get { return host.UdpEnabled; } }

        public static ClientInfo None = new ClientInfo(ISServerSocket.None, null);

        public void SetEdge(Edge e, ClientInfo info)
        {
            if(info.Name == "None")
                host.SetClientAtEdge(e, null);
            else
                host.SetClientAtEdge(e, clientMan.GetClientFromInfo(info));
        }

        public void SetHotkey(Hotkey hk)
        {
            host.CurrentHotkey = hk;
        }
    }
}
