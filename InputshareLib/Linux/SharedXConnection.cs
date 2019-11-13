using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static InputshareLib.Linux.Native.LibX11;
using static InputshareLib.Linux.Native.LibX11Events;

namespace InputshareLib.Linux
{
    public class SharedXConnection
    {
        public IntPtr XDisplay { get; private set; }
        public IntPtr XRootWindow { get; private set; }

        public event Action<XEvent> EventArrived;

        private Queue<XEvent> localEventQueue = new Queue<XEvent>();

        private X11ErrorDelegate errorHandler;
        private X11IOErrorDelegate ioErrorHandler;

        public SharedXConnection()
        {
            XInitThreads();
            XDisplay = XOpenDisplay(0);

            if (XDisplay == IntPtr.Zero)
                throw new XLibException("Failed to connect to X:");

            XRootWindow = XDefaultRootWindow(XDisplay);

            errorHandler = HandleError;
            ioErrorHandler = HandleIOError;

            XSetErrorHandler(errorHandler);
            XSetIOErrorHandler(ioErrorHandler);

            new Thread(() => { EventLoop(); }).Start();
        }


        public void EventLoop()
        {
            ISLogger.Write("Now waiting for events from X server");

            XSelectInput(XDisplay, XRootWindow, EventMask.PropertyChangeMask | EventMask.KeyPressMask);

            //TODO - poll raw socket properly.
            XEvent evt = new XEvent();
            while (true)
            {
                {
                    if (localEventQueue.Count > 0)
                        for (int i = 0; i < localEventQueue.Count; i++)
                        {
                            evt = localEventQueue.Dequeue();
                            EventArrived?.Invoke(evt);
                        }
                }

                if (XPending(XDisplay) > 0)
                {   
                    XNextEvent(XDisplay, ref evt);

                    EventArrived?.Invoke(evt);
                }
                else
                {
                    Thread.Sleep(5);
                    continue;
                }
            }
        }

        /// <summary>
        /// Adds an event that will be processed without being sent to X
        /// </summary>
        /// <param name="evt"></param>
        public void QueueLocalEvent(XEvent evt)
        {
            localEventQueue.Enqueue(evt);
        }

        private int HandleError(IntPtr display, ref XErrorEvent evt)
        {
            ISLogger.Write("------------------exception on X server!---------------------");

            StringBuilder sb = new StringBuilder(256);
            XGetErrorText(XDisplay, evt.error_code, sb, sb.Capacity);
            ISLogger.Write(sb.ToString());

            ISLogger.Write("------------------exception on X server!---------------------");
            return 0;
        }

        private int HandleIOError(IntPtr display)
        {
            ISLogger.Write("IO Error occurred on X server!");
            return 0;
        }

        ~SharedXConnection()
        {
            XCloseDisplay(XDisplay);
        }
    }
}
