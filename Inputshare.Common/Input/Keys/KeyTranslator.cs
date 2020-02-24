using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Input.Keys
{
    public static class KeyTranslator
    {
        public static LinuxKeyCode WindowsToLinux(WindowsVirtualKey key)
        {
            try
            {
                return (LinuxKeyCode)Enum.Parse(typeof(LinuxKeyCode), key.ToString());
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to translate windows key {0}: {1}", key, ex.Message);
                return 0;
            }
        }

        public static WindowsVirtualKey LinuxToWindows(LinuxKeyCode key)
        {
            try
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
            catch (Exception ex)
            {
                Logger.Warning("Failed to translate linux key {0}: {1}", key, ex.Message);
                return 0;
            }

        }
    }
}
