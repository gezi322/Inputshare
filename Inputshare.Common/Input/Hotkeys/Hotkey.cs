using Inputshare.Common.Input.Keys;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Input.Hotkeys
{
    public class Hotkey
    {
        public WindowsVirtualKey Key { get; }
        public KeyModifiers Modifiers { get; }
        public Hotkey(WindowsVirtualKey key, KeyModifiers mods)
        {
            Key = key;
            Modifiers = mods;
        }

        private Hotkey()
        {
            Key = WindowsVirtualKey.None;
            Modifiers = KeyModifiers.None;
        }

        public static Hotkey None = new Hotkey();

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

            return Key == hk2.Key && Modifiers == hk2.Modifiers;
        }

        public static bool operator == (Hotkey a, Hotkey b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
                return true;

            return a.Equals(b);
        }

        public static bool operator != (Hotkey a, Hotkey b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return $"{Modifiers}:{Key}";
        }

        /// <summary>
        /// Attempts to create a hotkey object from a string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryReadFromSettingsString(string value, out Hotkey result)
        {
            string[] args = value.Split(':');
            result = null;

            if (args.Length == 0)
                return false;

            if (!WindowsVirtualKey.TryParse(typeof(WindowsVirtualKey), args[0], true, out var keyObj))
                return false;

            WindowsVirtualKey key = (WindowsVirtualKey)keyObj;
            KeyModifiers mods = 0;

            for (int i = 1; i < args.Length; i++)
            {
                string modStr = args[i];

                if (modStr == KeyModifiers.Alt.ToString())
                    mods |= KeyModifiers.Alt;
                else if (modStr == KeyModifiers.Ctrl.ToString())
                    mods |= KeyModifiers.Ctrl;
                else if (modStr == KeyModifiers.Shift.ToString())
                    mods |= KeyModifiers.Shift;
            }

            result = new Hotkey(key, mods);
            return true;
        }

        /// <summary>
        /// Converts this hotkey object into a string
        /// </summary>
        /// <returns></returns>
        public string ToSettingsString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Key);
            foreach (var mod in (KeyModifiers[])Enum.GetValues(typeof(KeyModifiers)))
            {
                if(mod != KeyModifiers.None)
                    if (Modifiers.HasFlag(mod))
                        sb.Append(":" + mod);
            }

            return sb.ToString();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}
