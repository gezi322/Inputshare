using InputshareLib.Displays;
using InputshareLib.Input.Hotkeys;
using System;
using System.ComponentModel;
using System.Net;

namespace InputshareLib.Server.API
{
    /// <summary>
    /// Represents a client connected to the inputshare server (for use with a UI)
    /// </summary>
    public class ClientInfo
    {
        public ClientInfo(string name, Guid id, DisplayConfig displayConf, Hotkey clientHotkey, IPEndPoint clientAddress, bool udpEnabled)
        {
            Name = name;
            Id = id;
            DisplayConf = displayConf;
            ClientHotkey = clientHotkey;
            ClientAddress = clientAddress;
            UdpEnabled = udpEnabled;
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

        public ClientInfo LeftClient { get; internal set; }
        public ClientInfo RightClient { get; internal set; }
        public ClientInfo TopClient { get; internal set; }
        public ClientInfo BottomClient { get; internal set; }
        public bool InputClient { get; internal set; } = false;
        public string Name { get; }
        public Guid Id { get; }
        public DisplayConfig DisplayConf { get; }
        public Hotkey ClientHotkey { get; }
        public IPEndPoint ClientAddress { get; }
        public bool UdpEnabled { get; }
        public static ClientInfo None = new ClientInfo("None", Guid.NewGuid(), null, null, null, false);

        public static bool operator ==(ClientInfo a, ClientInfo b)
        {
            if (ReferenceEquals(a, null))
            {
                if (ReferenceEquals(b, null))
                {
                    return true;
                }
                return false;
            }
            if (ReferenceEquals(b, null))
            {
                if (ReferenceEquals(a, null))
                {
                    return true;
                }
                return false;
            }


            return a.Id == b.Id;
        }

        public static bool operator !=(ClientInfo a, ClientInfo b)
        {
            return !(a == b);
        }
    }
}
