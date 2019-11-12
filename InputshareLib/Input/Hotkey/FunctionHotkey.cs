using InputshareLib.Input.Keys;

namespace InputshareLib.Input.Hotkeys
{
    public class FunctionHotkey : Hotkey
    {
        public FunctionHotkey(WindowsVirtualKey key, HotkeyModifiers mods, Hotkeyfunction function) : base(key, mods)
        {
            Function = function;
        }

        public Hotkeyfunction Function { get; }
    }
}
