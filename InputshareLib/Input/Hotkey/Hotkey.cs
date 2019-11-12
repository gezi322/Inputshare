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

    }
}
