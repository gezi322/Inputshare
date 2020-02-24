using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11Structs;

namespace Inputshare.Common.PlatformModules.Linux.Native
{
    public static class LibXfixes
    {
        [DllImport("libXfixes.so.3")]
        public static extern void XFixesSelectSelectionInput(IntPtr display, IntPtr window, IntPtr atom, uint mask);

        [DllImport("libXfixes.so.3")]
        public static extern void XFixesSelectCursorInput(IntPtr display, IntPtr window, EventMask mask);

        [DllImport("libXfixes.so.3")]
        public static extern void XFixesHideCursor(IntPtr display, IntPtr window);
        [DllImport("libXfixes.so.3")]
        public static extern void XFixesShowCursor(IntPtr display, IntPtr window);

    }
}
