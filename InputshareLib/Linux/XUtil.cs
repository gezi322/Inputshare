using System;
using System.Collections.Generic;
using System.Text;
using static InputshareLib.Linux.Native.LibX11;
using static InputshareLib.Linux.Native.LibX11Events;

namespace InputshareLib.Linux
{
    internal static class XUtil
    {
        public static string GetWindowName(IntPtr xDisplay, IntPtr window)
        {
            XFetchName(xDisplay, window, out string name);
            return name;
        }

        public static string GetErrorString(IntPtr xDisplay, byte code)
        {
            StringBuilder sb = new StringBuilder(256);
            XGetErrorText(xDisplay, code, sb, sb.Capacity);
            return sb.ToString();
        }
    }
}
