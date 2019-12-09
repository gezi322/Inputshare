using System;
using System.Text;
using InputshareLib.Input.Keys;

namespace InputshareLib.Input.Hotkeys
{
    public class Hotkey
    {
        public WindowsVirtualKey Key { get; }
        public HotkeyModifiers Modifiers { get; }

        public Hotkey(WindowsVirtualKey key, HotkeyModifiers mods)
        {
            Key = key;
            Modifiers = mods;
        }

        public override bool Equals(object obj)
        {
            if (obj is Hotkey hk)
                return (hk.Key == Key && hk.Modifiers == Modifiers);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            if (Modifiers == 0 && Key == 0)
                return "None";


            return string.Format("{0} + {1}", Modifiers, Key);
        }

        public static bool operator ==(Hotkey hk1, Hotkey hk2)
        {
            if (ReferenceEquals(hk1, null))
            {
                if (ReferenceEquals(hk2, null))
                {
                    return true;
                }
                return false;
            }
            if (ReferenceEquals(hk2, null))
            {
                if (ReferenceEquals(hk1, null))
                {
                    return true;
                }
                return false;
            }


            return ((hk1.Key == hk2.Key) && (hk2.Modifiers == hk1.Modifiers));
        }
        public static bool operator !=(Hotkey hk1, Hotkey hk2)
        {
            return !(hk1 == hk2);
        }

        public string ToSettingsString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Key);
            foreach(var mod in (HotkeyModifiers[])Enum.GetValues(typeof(HotkeyModifiers)))
            {
                if (mod == HotkeyModifiers.Windows || mod == HotkeyModifiers.None)
                    continue;

                if(Modifiers.HasFlag(mod))
                    sb.Append(":" + mod);
            }

            return sb.ToString();
        }

        public static bool TryFromSettingsString(string hkStr, out Hotkey key)
        {
            key = FromSettingsString(hkStr);
            return key != null;
        }

        private static Hotkey FromSettingsString(string hkStr)
        {
            string[] args = hkStr.Split(':');

            if (args.Length == 0)
                return null;

            if (!WindowsVirtualKey.TryParse(typeof(WindowsVirtualKey), args[0], true, out var keyObj))
                return null;

            WindowsVirtualKey key = (WindowsVirtualKey)keyObj;
            HotkeyModifiers mods = 0;

            for(int i = 1; i < args.Length; i++)
            {
                string modStr = args[i];

                if (modStr == HotkeyModifiers.Alt.ToString())
                    mods |= HotkeyModifiers.Alt;
                else if (modStr == HotkeyModifiers.Ctrl.ToString())
                    mods |= HotkeyModifiers.Ctrl;
                else if (modStr == HotkeyModifiers.Shift.ToString())
                    mods |= HotkeyModifiers.Shift;
            }

            return new Hotkey(key, mods);
        }

    }
}
