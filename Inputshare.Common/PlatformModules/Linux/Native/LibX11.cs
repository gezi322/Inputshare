using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11Events;

namespace Inputshare.Common.PlatformModules.Linux.Native
{
    internal static class LibX11
    {
        public delegate int X11ErrorDelegate(IntPtr display, ref XErrorEvent error);
        public delegate int X11IOErrorDelegate(IntPtr display);

        private const string lib = "libX11.so.6";

        public const int X11_LEFTBUTTON = 1;
        public const int X11_MIDDLEBUTTON = 2;
        public const int X11_RIGHTBUTTON = 3;
        public const int X11_SCROLLUP = 4;
        public const int X11_SCROLLDOWN = 5;
        public const int X11_SCROSSLEFT = 6;
        public const int X11_SCROLLRIGHT = 7;
        public const int X11_XBUTTONBACK = 8;
        public const int X11_XBUTTONFORWARD = 9;


        [DllImport(lib)]
        public static extern void XWarpPointer(IntPtr display, IntPtr src_w, IntPtr dest_w, int src_x, int src_y, uint src_width, uint src_height, int dest_x, int dest_y);
        [DllImport(lib)]
        public static extern IntPtr XFlush(IntPtr display);

        [DllImport(lib)]
        public static extern IntPtr XInitThreads();
        [DllImport(lib)]
        public static extern int XSetIOErrorHandler(X11IOErrorDelegate handler);

        [DllImport(lib)]
        public static extern int XSetErrorHandler(X11ErrorDelegate handler);

        [DllImport(lib)]
        public static extern int XNextEvent(IntPtr display, ref XEvent event_return);
        [DllImport(lib)]
        public static extern int XPending(IntPtr display);

        [DllImport(lib)]
        public static extern void XGetErrorText(IntPtr display, byte code, StringBuilder buffer, int len);

        [DllImport(lib)]
        public static extern IntPtr XOpenDisplay(int scr);


    }
}
