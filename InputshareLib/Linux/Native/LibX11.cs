using InputshareLib.Input;
using InputshareLib.Input.Keys;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static InputshareLib.Linux.Native.LibX11Events;

namespace InputshareLib.Linux.Native
{
    public static class LibX11
    {
        public delegate int X11ErrorDelegate(IntPtr display, ref XErrorEvent error);
        public delegate int X11IOErrorDelegate(IntPtr display);

        [Flags]
        public enum MouseKeyMasks
        {
            Button1Mask = (1 << 8),
            Button2Mask = (1 << 9),
            Button3Mask = (1 << 10),
            Button4Mask = (1 << 11),
            Button5Mask = (1 << 12),
        }
        [DllImport("libX11.so.6")]
        public static extern int XSynchronize(IntPtr display, bool sync);

        [DllImport("libX11.so.6")]
        public static extern int XConnectionNumber(IntPtr display);

        [DllImport("libX11.so.6")]
        public static extern string XKeysymToString(int keySym);

        [DllImport("libX11.so.6")]
        public static extern int XKeycodeToKeysym(IntPtr display, int keycode, int index);

        [DllImport("libX11.so.6")]
        public static extern int XUngrabKey(IntPtr display, LinuxKeyCode key, LinuxKeyMask mask, IntPtr window);

        [DllImport("libX11.so.6")]
        public static extern int XSetIOErrorHandler(X11IOErrorDelegate handler);

        [DllImport("libX11.so.6")]
        public static extern bool XCheckIfEvent(IntPtr display, ref XEvent evt_ret, Delegate predicate, IntPtr arg);

        [DllImport("libX11.so.6")]
        public static extern bool XIfEvent(IntPtr display, ref XEvent evt_ret, Delegate predicate, IntPtr arg);

        [DllImport("libX11.so.6")]
        public static extern int XPutBackEvent(IntPtr display, XEvent evt);

        [DllImport("libX11.so.6")]
        public static extern int XMaskEvent(IntPtr display, EventMask mask, out XEvent event_return);

        [DllImport("libX11.so.6")]
        public static extern int XCheckMaskEvent(IntPtr display, EventMask mask, out XEvent returnEvent);

        [DllImport("libX11.so.6")]
        public static extern int XMapRaised(IntPtr display, IntPtr window);

        [DllImport("libX11.so.6")]
        public static extern void XUnlockDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        public static extern void XMoveWindow(IntPtr display, IntPtr window, int x, int y);

        [DllImport("libX11.so.6")]
        public static extern void XUnmapWindow(IntPtr display, IntPtr window);

        [DllImport("libX11.so.6")]
        public static extern int XSetInputFocus(IntPtr display, IntPtr focus, int revert_to, IntPtr time);

        [DllImport("libX11.so.6")]
        public static extern void XUngrabPointer(IntPtr display, IntPtr time);

        [DllImport("libX11.so.6")]
        public static extern void XStoreName(IntPtr display, IntPtr window, string window_name);

        [DllImport("libX11.so.6")]
        public static extern void XGetErrorText(IntPtr display, int code, byte[] return_buffer, int len);

        [DllImport("libX11.so.6")]
        public static extern void XGetErrorText(IntPtr display, byte code, StringBuilder buffer, int len);

        [DllImport("libX11.so.6")]
        public static extern int XSetErrorHandler(X11ErrorDelegate handler);

        [DllImport("libX11.so.6")]
        public static extern void XDestroyWindow(IntPtr display, IntPtr window);

        [DllImport("libX11.so.6")]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, IntPtr[] data, int nelements);

        [DllImport("libX11.so.6")]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, long[] data, int nelements);
        [DllImport("libX11.so.6")]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, byte[] data, int nelements);

        [DllImport("libX11.so.6")]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, string data, int nelements);

        [DllImport("libX11.so.6")]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, int[] data, int nelements);

        [DllImport("libX11.so.6")]
        public static extern string XGetAtomName(IntPtr display, IntPtr atom);
        [DllImport("libX11.so.6")]
        public static extern void XSync(IntPtr display, bool discard);
        [DllImport("libX11.so.6")]
        public static extern int XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, IntPtr time = default);

        [DllImport("libXfixes.so.3")]
        public static extern void XFixesSelectSelectionInput(IntPtr display, IntPtr window, IntPtr atom, uint mask);

        [DllImport("libX11.so.6")]
        public static extern void XDeleteProperty(IntPtr display, IntPtr window, IntPtr prop);

        [DllImport("libX11.so.6")]
        public static extern int XGetWindowProperty(IntPtr display, IntPtr window, IntPtr property, int offset, int len, bool delete,
            IntPtr req_type, out IntPtr actual_type_return, out int actual_format_return, out int nitems_return, out int bytes_after_return, out IntPtr prop_return);

        [DllImport("libX11.so.6")]
        public static extern void XConvertSelection(IntPtr display, IntPtr selectionAtom, IntPtr targetAtom, IntPtr propertyAtom, IntPtr requestor, IntPtr time);

        [DllImport("libX11.so.6")]
        public static extern int XSendEvent(IntPtr display, IntPtr window, bool propagate, EventMask mask, ref XEvent evt);
        [DllImport("libX11.so.6")]
        public static extern void XFree(IntPtr data);

        [DllImport("libX11.so.6")]
        public static extern IntPtr XFetchName(IntPtr display, IntPtr window, out string window_name_return);

        [DllImport("libX11.so.6")]
        public static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

        [DllImport("libX11.so.6")]
        public static extern IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);

        [DllImport("libX11.so.6")]
        public static extern void XUngrabServer(IntPtr display);

        [DllImport("libX11.so.6")]
        public static extern void XGrabServer(IntPtr display);

        [DllImport("libX11.so.6")]
        public static extern void XUngrabKeyboard(IntPtr display, IntPtr time);


        [DllImport("libX11.so.6")]
        public static extern int XKeysymToKeycode(IntPtr display, int keysym);

        public const int X11_LEFTBUTTON = 1;
        public const int X11_MIDDLEBUTTON = 2;
        public const int X11_RIGHTBUTTON = 3;
        public const int X11_SCROLLUP = 4;
        public const int X11_SCROLLDOWN = 5;
        public const int X11_SCROSSLEFT = 6;
        public const int X11_SCROLLRIGHT = 7;
        public const int X11_XBUTTONBACK = 8;
        public const int X11_XBUTTONFORWARD = 9;


        [DllImport("libX11.so.6")]
        public static extern int XPeekEvent(IntPtr display, ref XEvent event_return);

        [DllImport("libXtst.so.6")]
        public static extern int XTestFakeMotionEvent(IntPtr display, int screenNumber, int x, int y, uint delay);

        [DllImport("libXtst.so.6")]
        public static extern int XGrabKey(IntPtr display, LinuxKeyCode key, LinuxKeyMask mask, IntPtr window, bool owner_events, int pointerMode, int keyboardMode);

        [DllImport("libXtst.so.6")]
        public static extern int XGrabKeyboard(IntPtr display,
        IntPtr grab_window,
        bool owner_events,
        int pointer_mode, int keyboard_mode,
        IntPtr time);
        [DllImport("libXtst.so.6")]
        public static extern int XTestFakeRelativeMotionEvent(IntPtr display, int screenNumber, int x, int y, uint delay);

        [DllImport("libX11.so.6")]
        public static extern int XAllowEvents(IntPtr display, AllowEventMode event_mode, IntPtr time);

        public enum AllowEventMode
        {
            AsyncPointer = 0,
            SyncPointer,
            ReplayPointer,
            AsyncKeyboard,
            SyncKeyboard,
            ReplayKeyboard,
            AsyncBoth,
            SyncBoth
        }

        [DllImport("libX11.so.6")]
        public static extern int XPending(IntPtr display);

        [DllImport("libX11.so.6")]
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

        [DllImport("libX11.so.6")]
        public static extern void XGetInputFocus(IntPtr display, out IntPtr focus_return, out IntPtr revert_to_return);

        [DllImport("libX11.so.6")]
        public static extern IntPtr XWhitePixel(IntPtr display, int screen);

        [DllImport("libX11.so.6")]
        public static extern IntPtr XDefaultVisual(IntPtr display, int screen);

        [DllImport("libX11.so.6")]
        public static extern IntPtr XGetVisualInfo(IntPtr display, int mask, ref XVisualInfo visInfo, out int nItems);

        [DllImport("libX11.so.6")]
        public static extern IntPtr XCreateColormap(IntPtr display, IntPtr window, IntPtr visual, int alloc);

        [DllImport("libX11.so.6")]
        public static extern IntPtr XCreateWindow(IntPtr display, IntPtr parent,
           int x, int y, int width, int height, int border_width,
           int depth, XWindowType cls, IntPtr visual, IntPtr valueMask, ref XSetWindowAttributes attributes);

        [DllImport("libX11.so.6")]
        public static extern int XNextEvent(IntPtr display, ref XEvent event_return);

        [DllImport("libXtst.so.6")]
        public static extern int XMapWindow(IntPtr display, IntPtr window);
        [DllImport("libX11.so.6")]
        public static extern void XSelectInput(IntPtr display, IntPtr window, EventMask mask);

        [DllImport("libX11.so.6")]
        public extern static IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, int width, int height, int border_width, UIntPtr border, UIntPtr background);

        [DllImport("libX11.so.6")]
        public static extern int XDefaultScreen(IntPtr display);

        [DllImport("libX11.so.6")]
        public static extern IntPtr XInitThreads();

        [DllImport("libX11.so.6")]
        public static extern IntPtr XOpenDisplay(int scr);

        [DllImport("libX11.so.6")]
        public static extern void XCloseDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        public static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport("libXtst.so.6")]
        public static extern void XWarpPointer(IntPtr display, IntPtr src_w, IntPtr dest_w, int src_x, int src_y, uint src_width, uint src_height, int dest_x, int dest_y);

        [DllImport("libX11.so.6")]
        public static extern IntPtr XFlush(IntPtr display);

        [DllImport("libXtst.so.6")]
        public static extern int XTestFakeButtonEvent(IntPtr display, int button, bool pressed, int delay);

        [DllImport("libXtst.so.6")]
        public static extern int XTestFakeKeyEvent(IntPtr display, uint keycode, bool pressed, int delay);

        [DllImport("libX11.so.6")]
        public extern static bool XQueryPointer(IntPtr display, IntPtr window, out IntPtr root, out IntPtr child, out int root_x, out int root_y, out int win_x, out int win_y, out int keys_buttons);

        [DllImport("libX11.so.6")]
        public static extern bool XGetWindowAttributes(IntPtr display, IntPtr w, out XWindowAttributes window_attributes_return);

        public struct XImage
        {
            public int width;
            public int height;
            public int format;
            [MarshalAs(UnmanagedType.ByValArray)] public byte[] data;
            public int byte_order;
            public int bitmap_bit_order;
            public int bitmap_pad;
            public int depth;
            public int red_mask;
            public int green_mask;
            public int blue_mask;
            public IntPtr obdata;
            //todo
        }




        [StructLayout(LayoutKind.Sequential)]
        public struct XWindowAttributes
        {
            public int x;
            public int y;
            public int width;
            public int height;
            public int border_width;
            public int depth;
            public IntPtr visual;
            public IntPtr root;
            public int c_class;
            public Gravity bit_gravity;
            public Gravity win_gravity;
            public int backing_store;
            public IntPtr backing_planes;
            public IntPtr backing_pixel;
            public bool save_under;
            public IntPtr colormap;
            public bool map_installed;
            public MapState map_state;
            public IntPtr all_event_masks;
            public IntPtr your_event_mask;
            public IntPtr do_not_propagate_mask;
            public bool override_direct;
            public IntPtr screen;
        }
        public enum MapState
        {
            IsUnmapped = 0,
            IsUnviewable = 1,
            IsViewable = 2
        }

        public enum Gravity
        {
            ForgetGravity = 0,
            NorthWestGravity = 1,
            NorthGravity = 2,
            NorthEastGravity = 3,
            WestGravity = 4,
            CenterGravity = 5,
            EastGravity = 6,
            SouthWestGravity = 7,
            SouthGravity = 8,
            SouthEastGravity = 9,
            StaticGravity = 10
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XSetWindowAttributes
        {
            public IntPtr background_pixmap;
            public IntPtr background_pixel;
            public IntPtr border_pixmap;
            public IntPtr border_pixel;
            public Gravity bit_gravity;
            public Gravity win_gravity;
            public int backing_store;
            public IntPtr backing_planes;
            public IntPtr backing_pixel;
            public bool save_under;
            public EventMask event_mask;
            public EventMask do_not_propagate_mask;
            public bool override_redirect;
            public IntPtr colormap;
            public IntPtr cursor;
        }

        public enum XWindowType
        {
            InputOutput = 1,
            InputOnly = 2,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XVisualInfo
        {
            public IntPtr Visual;
            public IntPtr VisualID;
            public int Screen;
            public int Depth;
            public int Class;
            public long RedMask;
            public long GreenMask;
            public long blueMask;
            public int ColormapSize;
            public int BitsPerRgb;

            public override string ToString()
            {
                return String.Format("id ({0}), screen ({1}), depth ({2}), class ({3})",
                    VisualID, Screen, Depth, Class);
            }
        }
    }
}
