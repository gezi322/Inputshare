using Inputshare.Common.Input.Keys;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Input.Hotkeys
{
    public class Hotkey
    {
        public WindowsVirtualKey Key { get; }
        public KeyModifiers Mods { get; }
        public Hotkey(WindowsVirtualKey key, KeyModifiers mods)
        {
            Key = key;
            Mods = mods;
        }

        public static KeyModifiers CreateKeyModifiers(bool alt, bool ctrl, bool shift, bool win)
        {
            KeyModifiers mods = 0;

            if (alt)
                mods |= KeyModifiers.Alt;
            if (ctrl)
                mods |= KeyModifiers.Ctrl;
            if (shift)
                mods |= KeyModifiers.Shift;
            if (win)
                mods |= KeyModifiers.Win;

            return mods;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is Hotkey hk2))
                return false;

            return Key == hk2.Key && Mods == hk2.Mods;
        }

        public override string ToString()
        {
            return $"{Mods}:{Key}";
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}
