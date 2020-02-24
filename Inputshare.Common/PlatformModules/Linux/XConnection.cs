using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11Structs;
using static Inputshare.Common.PlatformModules.Linux.Native.Libc;
using static Inputshare.Common.PlatformModules.Linux.Native.LibXfixes;
using System.Threading.Tasks;

namespace Inputshare.Common.PlatformModules.Linux
{
    /// <summary>
    /// An Xconnection that is shared between X11 modules
    /// </summary>
    public class XConnection : IPlatformDependency
    {
        internal event EventHandler<XEvent> EventReceived;
        internal IntPtr XDisplay;
        internal IntPtr XInvokeEventWindow;

        private X11ErrorDelegate errorHandler;
        private X11IOErrorDelegate ioErrorHandler;

        public XConnection()
        {
            XInitThreads();
            XDisplay = XOpenDisplay(0);

            if (XDisplay == IntPtr.Zero)
                throw new X11Exception("Failed to open XDisplay");

            errorHandler = new X11ErrorDelegate(HandleError);
            ioErrorHandler = new X11IOErrorDelegate(HandleIOError);
            XSetErrorHandler(errorHandler);
            XSetIOErrorHandler(ioErrorHandler);

            Task.Run(() => XMessageLoop());
        }

        private void XMessageLoop()
        {
            XInvokeEventWindow = XCreateSimpleWindow(XDisplay, XDefaultRootWindow(XDisplay), 0, 0, 1, 1, 0, UIntPtr.Zero, UIntPtr.Zero);
            //Select structurenotifymask on root window to received configure events when display size changes
            XSelectInput(XDisplay, XDefaultRootWindow(XDisplay), EventMask.StructureNotifyMask);
            XSelectInput(XDisplay, XInvokeEventWindow, EventMask.PropertyChangeMask | EventMask.PointerMotionMask | EventMask.StructureNotifyMask);
            XEvent evt = new XEvent();

            while (!disposedValue)
            {
                try
                {
                    timeval v = new timeval();
                    v.tv_usec = 1000; //1 MS poll

                    int num = select(0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref v);

                    while (XPending(XDisplay) > 0)
                    {
                        XNextEvent(XDisplay, ref evt);
                        EventReceived?.Invoke(this, evt);
                    }
                }catch(Exception ex)
                {
                    Logger.Error($"XConnection exception in message loop: {ex.Message}");
                }
                
            }

            Logger.Information("Closed X connection");
        }

        private int HandleError(IntPtr display, ref XErrorEvent evt)
        {
            Logger.Error("X11 ERROR!"); 
            Logger.Error("request: " + evt.request_code);
            Logger.Error("minor code " + evt.minor_code);
            Logger.Error("error code " + evt.error_code);
            return 0;
        }

        private int HandleIOError(IntPtr display)
        {
            Logger.Fatal("X IO Error!");
            return 0;
        }

        private bool disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }

    }
}
