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
            if (key == LinuxKeyCode.KPRETURN)
                key = LinuxKeyCode.Return;
            else if (key == LinuxKeyCode.KPDELETE)
                key = LinuxKeyCode.Delete;
            else if (key == LinuxKeyCode.KPSUBTRACT)
                key = LinuxKeyCode.Subtract;
            else if (key == LinuxKeyCode.KPPLUS)
                key = LinuxKeyCode.Add;

            return (WindowsVirtualKey)Enum.Parse(typeof(WindowsVirtualKey), key.ToString());
        }
    }
}
