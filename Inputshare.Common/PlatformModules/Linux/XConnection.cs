using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11Events;
using static Inputshare.Common.PlatformModules.Linux.Native.Libc;
using System.Threading.Tasks;

namespace Inputshare.Common.PlatformModules.Linux
{
    internal class XConnection : IPlatformDependency
    {
        internal event EventHandler<XEvent> EventReceived;
        internal IntPtr XDisplay;
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

            //Task.Run(() => XMessageLoop());
            //_xThread = new Thread(XMessageLoop);
            //_xThread.SetApartmentState(ApartmentState.STA);
            //_xThread.Start();
        }

        private void XMessageLoop()
        {
            XEvent evt = new XEvent();
            while (!disposedValue)
            {
                timeval v = new timeval();
                v.tv_usec = 1000; //1 MS poll

                int num = select(0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref v);

                while (XPending(XDisplay) > 0)
                {
                    XNextEvent(XDisplay, ref evt);
                    EventReceived?.Invoke(this, evt);
                }
            }
        }

        private int HandleError(IntPtr display, ref XErrorEvent evt)
        {
            throw X11Exception.Create(evt.error_code, XDisplay);
        }

        private int HandleIOError(IntPtr display)
        {
            throw new X11Exception("X IO error!");
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
