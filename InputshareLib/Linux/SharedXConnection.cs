using InputshareLib.PlatformModules.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static InputshareLib.Linux.Native.LibX11;
using static InputshareLib.Linux.Native.LibX11Events;
using static InputshareLib.Linux.Native.Libc;
using System.Runtime.InteropServices;

namespace InputshareLib.Linux
{
    public class SharedXConnection
    {
        public IntPtr XDisplay { get; private set; }
        public IntPtr XRootWindow { get; private set; }
        public IntPtr XCbWindow { get; private set; }

        public event Action<XEvent> EventArrived;
        private X11ErrorDelegate errorHandler;
        private X11IOErrorDelegate ioErrorHandler;

        public SharedXConnection()
        {
            //Using a standard loop of XNextEvent was causing unpredicatble issues, 
            //instead we poll the X server for events every few MS. This has shown
            //much more predictable results

            XInitThreads();
            XDisplay = XOpenDisplay(0);

            if (XDisplay == IntPtr.Zero)
                throw new XLibException("Failed to connect to X");

            XRootWindow = XDefaultRootWindow(XDisplay);

            errorHandler = HandleError;
            ioErrorHandler = HandleIOError;

            XSetErrorHandler(errorHandler);
            XSetIOErrorHandler(ioErrorHandler);

            new Thread(() => { EventLoop(); }).Start();
        }

        public void EventLoop()
        {
            XCbWindow = XCreateSimpleWindow(XDisplay, XRootWindow, 0, 0, 1, 1, 0, UIntPtr.Zero, UIntPtr.Zero);
            XFlush(XDisplay);
            XSelectInput(XDisplay, XCbWindow, EventMask.PropertyChangeMask | EventMask.KeyPressMask | EventMask.PointerMotionMask);

            int timeout_usec = Settings.XServerPollRateMS * 1000;
            
            //TODO - poll raw socket properly.
            XEvent evt = new XEvent();
            while (true)
            {
                timeval v = new timeval();
                v.tv_usec = timeout_usec;

                int num = select(0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref v);

                while(XPending(XDisplay) > 0){
                    XNextEvent(XDisplay, ref evt);

                    EventArrived?.Invoke(evt);
                }
            }
        }

        private int HandleError(IntPtr display, ref XErrorEvent evt)
        {
            ISLogger.Write("------------------------X ERROR--------------------------");
            ISLogger.Write("REQUEST = " + evt.request_code);
            ISLogger.Write("MINOR CODE = " + evt.minor_code);
            ISLogger.Write("CODE = " + evt.error_code);
            ISLogger.Write("MESSAGE = {0}", GetErrorString(evt.error_code));
            ISLogger.Write("------------------------X ERROR--------------------------");
            return 0;
        }

        private string GetErrorString(int code)
        {
            StringBuilder sb = new StringBuilder(160);
            XGetErrorText(XDisplay, (byte)code, sb, sb.Capacity);
            return sb.ToString();
        }

        private int HandleIOError(IntPtr display)
        {
            ISLogger.Write("IO Error occurred on X server!");
            return 0;
        }
    }
}
