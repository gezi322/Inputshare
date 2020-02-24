using Inputshare.Common.Input.Keys;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11Structs;

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
        public static extern int XUngrabKey(IntPtr display, LinuxKeyCode key, LinuxKeyMask mask, IntPtr window);
        [DllImport(lib)]
        public extern static IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, int width, int height, int border_width, UIntPtr border, UIntPtr background);

        [DllImport(lib)]
        public static extern IntPtr XCreateWindow(IntPtr display, IntPtr parent,
           int x, int y, int width, int height, int border_width,
           int depth, XWindowType cls, IntPtr visual, IntPtr valueMask, ref XSetWindowAttributes attributes);

        [DllImport(lib)]
        public static extern int XMaskEvent(IntPtr display, EventMask mask, out XEvent event_return);


        [DllImport(lib)]
        public static extern void XUngrabPointer(IntPtr display, IntPtr time);

        [DllImport(lib)]
        public static extern void XUngrabKeyboard(IntPtr display, IntPtr time);

        [DllImport(lib)]
        public static extern int XGrabKey(IntPtr display, LinuxKeyCode key, LinuxKeyMask mask, IntPtr window, bool owner_events, int pointerMode, int keyboardMode);
        
        [DllImport(lib)]
        public static extern int XGrabPointer(IntPtr display,
          IntPtr window,
          bool owner_events,
          EventMask event_mask,
          int pointer_mode,
          int keyboard_mode,
          IntPtr confine_to,
          IntPtr cursor,
          IntPtr time
      );

        [DllImport(lib)]
        public static extern int XGrabKeyboard(IntPtr display,
        IntPtr grab_window,
        bool owner_events,
        int pointer_mode, int keyboard_mode,
        IntPtr time);

        [DllImport(lib)]
        public static extern int XSendEvent(IntPtr display, IntPtr window, bool propagate, EventMask mask, ref XEvent evt);

        [DllImport(lib)]
        public static extern bool XGetWindowAttributes(IntPtr display, IntPtr w, out XWindowAttributes window_attributes_return);

        [DllImport(lib)]
        public extern static bool XQueryPointer(IntPtr display, IntPtr window, out IntPtr root, out IntPtr child, out int root_x, out int root_y, out int win_x, out int win_y, out int keys_buttons);

        [DllImport(lib)]
        public static extern void XSelectInput(IntPtr display, IntPtr window, EventMask mask);

        [DllImport(lib)]
        public static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

        [DllImport(lib)]
        public static extern IntPtr XDefaultRootWindow(IntPtr display);

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
