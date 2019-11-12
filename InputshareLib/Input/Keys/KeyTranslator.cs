using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Input.Keys
{
    public static class KeyTranslator
    {
        public static LinuxKeyCode WindowsToLinux(WindowsVirtualKey key)
        {
            return (LinuxKeyCode)Enum.Parse(typeof(LinuxKeyCode), key.ToString());
        }
        public static WindowsVirtualKey LinuxToWindows(LinuxKeyCode key)
        {
            return (WindowsVirtualKey)Enum.Parse(typeof(WindowsVirtualKey), key.ToString());
        }
    }
}
