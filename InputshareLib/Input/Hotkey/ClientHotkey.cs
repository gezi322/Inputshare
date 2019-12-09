using InputshareLib.Input.Keys;
using System;

namespace InputshareLib.Input.Hotkeys
{
    public class ClientHotkey : Hotkey
    {
        public ClientHotkey(WindowsVirtualKey key, HotkeyModifiers mods, Guid targetClient) : base(key, mods)
        {
            TargetClient = targetClient;
        }

        public Guid TargetClient { get; }
    }
}
