using InputshareLib.Input.Hotkeys;
using System;
using System.Net;
using static InputshareLib.Displays.DisplayManagerBase;

namespace InputshareLib.Server.API
{
    /// <summary>
    /// Represents a client connected to the inputshare server (for use with a UI)
    /// </summary>
    public class ClientInfo
    {
        public ClientInfo(string name, Guid id, DisplayConfig displayConf, Hotkey clientHotkey, IPEndPoint clientAddress)
        {
            Name = name;
            Id = id;
            DisplayConf = displayConf;
            ClientHotkey = clientHotkey;
            ClientAddress = clientAddress;
        }

        public override string ToString()
        {
            return Name;
        }

        public ClientInfo LeftClient { get; set; }
        public ClientInfo RightClient { get; set; }
        public ClientInfo TopClient { get; set; }
        public ClientInfo BottomClient { get; set; }
        public bool InputClient { get; internal set; } = false;
        public string Name { get; }
        public Guid Id { get; }
        public DisplayConfig DisplayConf { get; }
        public Hotkey ClientHotkey { get; }
        public IPEndPoint ClientAddress { get; }

        public static ClientInfo None { get => new ClientInfo("None", Guid.NewGuid(), null, null, null); }
    }
}
