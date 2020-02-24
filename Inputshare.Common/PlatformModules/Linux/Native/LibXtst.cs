using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Inputshare.Common.PlatformModules.Linux.Native
{
    internal static class LibXtst
    {
        private const string lib = "libXtst.so.6";

        [DllImport(lib)]
        public static extern int XTestFakeKeyEvent(IntPtr display, uint keycode, bool pressed, int delay);


        [DllImport(lib)]
        public static extern int XTestFakeButtonEvent(IntPtr display, int button, bool pressed, int delay);

        [DllImport(lib)]
        public static extern int XTestFakeRelativeMotionEvent(IntPtr display, int screenNumber, int x, int y, uint delay);

        [DllImport(lib)]
        public static extern int XTestFakeMotionEvent(IntPtr display, int screenNumber, int x, int y, uint delay);
    }
}
