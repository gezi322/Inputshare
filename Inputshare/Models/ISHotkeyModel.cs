using InputshareLib.Input.Hotkeys;
using InputshareLib.Input.Keys;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Inputshare.Models
{
    internal sealed class ISHotkeyModel
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }

        public Avalonia.Input.Key Key { get; set; }

        public ISHotkeyModel()
        {

        }

        public ISHotkeyModel(Hotkey hk)
        {
            Ctrl = hk.Modifiers.HasFlag(HotkeyModifiers.Ctrl);
            Alt = hk.Modifiers.HasFlag(HotkeyModifiers.Alt);
            Shift = hk.Modifiers.HasFlag(HotkeyModifiers.Shift);

#if WindowsBuild
            Key = (Avalonia.Input.Key)KeyInterop.KeyFromVirtualKey((int)hk.Key);
# else
            Key = (Avalonia.Input.Key)Enum.Parse(typeof(Avalonia.Input.Key), hk.Key.ToString());
#endif
        }

        public override string ToString()
        {
            return Key.ToString();
        }
    }
}
