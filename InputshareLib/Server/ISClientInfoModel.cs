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
    public class ISClientInfoModel
    {
        public event EventHandler<string> Disconnected;

        private ISServerSocket host;
        private ClientManager clientMan;

        internal ISClientInfoModel(ISServerSocket client, ClientManager clientManager)
        {
            clientMan = clientManager;
            host = client;

            if (host == null)
                host = ISServerSocket.Localhost;

            host.ConnectionError += Disconnected;
        }

        public override bool Equals(object obj)
        {
            if(obj is ISClientInfoModel b)
            {
                return b.Id == Id;
            }

            return false;
        }

        public static bool operator ==(ISClientInfoModel c1, ISClientInfoModel c2)
        {
            if (ReferenceEquals(c1, null))
            {
                if (ReferenceEquals(c2, null))
                {
                    return true;
                }
                return false;
            }
            if (ReferenceEquals(c2, null))
            {
                if (ReferenceEquals(c1, null))
                {
                    return true;
                }
                return false;
            }

            return c1.Id == c2.Id;
        }

        public static bool operator !=(ISClientInfoModel c1, ISClientInfoModel c2)
        {
            return !(c1 == c2);
        }

        public override string ToString()
        {
            return Name;
        }

        public ISClientInfoModel LeftClient { 
            get 
            { 
                return host.LeftClient == null ? ISClientInfoModel.None : new ISClientInfoModel(host.LeftClient, clientMan); 
            }
            set
            {
                SetEdge(Edge.Left, value);
            }
        }

        public ISClientInfoModel RightClient
        {
            get
            {
                return host.RightClient == null ? ISClientInfoModel.None : new ISClientInfoModel(host.RightClient, clientMan);
            }
            set
            {
                SetEdge(Edge.Right, value);
            }
        }
        public ISClientInfoModel TopClient
        {
            get
            {
                return host.TopClient == null ? ISClientInfoModel.None : new ISClientInfoModel(host.TopClient, clientMan);
            }
            set
            {
                SetEdge(Edge.Top, value);
            }
        }
        public ISClientInfoModel BottomClient
        {
            get
            {
                return host.BottomClient == null ? ISClientInfoModel.None : new ISClientInfoModel(host.BottomClient, clientMan);
            }
            set
            {
                SetEdge(Edge.Bottom, value);
            }
        }

        public string Name { get { return host.ClientName; } }
        public Guid Id { get { return host.ClientId; } }
        public Hotkey ClientHotkey { get { return host.CurrentHotkey == null ? new Hotkey(0, 0) : host.CurrentHotkey; } } 
        public IPEndPoint ClientAddress { get { return host.ClientEndpoint == null ? new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0) : host.ClientEndpoint; } }
        public bool UdpEnabled { get { return host.UdpEnabled; } }

        public static ISClientInfoModel None = new ISClientInfoModel(ISServerSocket.None, null);

        public void SetEdge(Edge e, ISClientInfoModel info)
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
