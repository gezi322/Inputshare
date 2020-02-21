using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.PlatformModules.Windows.Native
{
    internal struct Win32Message
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public int time;
        public User32.POINT pt;
    }
}
