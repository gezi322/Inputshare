using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InputshareLib.Clipboard;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Input;
using InputshareLib.Input.Keys;
using InputshareLib.Linux;
using InputshareLib.PlatformModules.Input;
using static InputshareLib.Linux.Native.LibX11;
using static InputshareLib.Linux.Native.LibX11Events;

namespace InputshareLib.PlatformModules.Input
{
    public class LinuxInputManager : InputManagerBase
    {
        public override bool LeftMouseDown { get; }

        public override bool InputBlocked { get; protected set; }
        public override MouseInputMode MouseRecordMode { get; protected set; }

        public override event EventHandler<ISInputData> InputReceived;

        private Thread wndThread;

        private IntPtr atomGrab;
        private IntPtr atomUngrab;
        private IntPtr atomIgnoreNext;
        private int storedX = 0;
        private int storedY = 0;
        private bool[] buttonStates = new bool[16];

        private bool _stopGrab = false;

        private SharedXConnection xConnection;
        private IntPtr xWindow;

        private EventMask anyMotionMask = EventMask.Button1MotionMask | EventMask.Button2MotionMask | EventMask.Button3MotionMask | EventMask.Button4MotionMask
                | EventMask.Button5MotionMask | EventMask.ButtonMotionMask | EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask;

        private AutoResetEvent windowCreateEvent = new AutoResetEvent(false);

        public LinuxInputManager(SharedXConnection xCon)
        {
            xConnection = xCon;
        }
        public override void SetInputBlocked(bool block)
        {
            InvokeGrabInput(block);
        }

        public override void SetMouseInputMode(MouseInputMode mode, int interval = 0)
        {

        }

        protected override void OnStart()
        {
            wndThread = new Thread(WindowThread);
            wndThread.Start();

            if (!windowCreateEvent.WaitOne(2000))
                throw new XLibException("Timed out waiting for window create event");

        }

        protected override void OnStop()
        {
            XDestroyWindow(xConnection.XRootWindow, xWindow);
        }

        private void WindowThread()
        {
            atomGrab = XInternAtom(xConnection.XDisplay, "ISGrab", false);
            atomUngrab = XInternAtom(xConnection.XDisplay, "ISUngrab", false);
            atomIgnoreNext = XInternAtom(xConnection.XDisplay, "ISIgnoreNext", false);
            XSetWindowAttributes attribs = new XSetWindowAttributes();
            attribs.event_mask = anyMotionMask | EventMask.PropertyChangeMask;
            xWindow = XCreateWindow(xConnection.XDisplay, xConnection.XRootWindow, 0, 0, 100, 100, 0, 0, XWindowType.InputOnly, IntPtr.Zero, IntPtr.Zero, ref attribs);
            XStoreName(xConnection.XDisplay, xWindow, "ISInputWindow");
            //xWindow = XCreateSimpleWindow(xConnection.XDisplay, XDefaultRootWindow(xConnection.XDisplay), 0, 0, 100, 100, 20, UIntPtr.Zero, UIntPtr.Zero);
            //XFlush(xConnection.XDisplay);

            XSelectInput(xConnection.XDisplay, xWindow, EventMask.PropertyChangeMask | anyMotionMask | EventMask.ExposureMask | EventMask.StructureNotifyMask);
            ISLogger.Write("Window created");

            windowCreateEvent.WaitOne();

            xConnection.EventArrived += XConnection_EventArrived;
        }

        private void XConnection_EventArrived(XEvent evt)
        {
            if (evt.type == XEventName.ClientMessage)
                if (evt.ClientMessageEvent.ptr1 == atomIgnoreNext)
                {
                    if (XPending(xConnection.XDisplay) > 0)
                        XNextEvent(xConnection.XDisplay, ref evt);
                }
            ProcEvent(evt);
        }

        private void ProcEvent(XEvent evt)
        {
            if (evt.type == XEventName.MotionNotify && (evt.MotionEvent.x_root == lastX && evt.MotionEvent.y_root == lastY))
            {
                return;
            }

            if (evt.type == XEventName.MotionNotify)
                HandleMotionEvent(evt.MotionEvent);
            if (evt.type == XEventName.ButtonPress)
                HandleButtonPressEvent(evt.ButtonEvent);
            else if (evt.type == XEventName.ButtonRelease)
                HandleButtonReleaseEvent(evt.ButtonEvent);
            else if (evt.type == XEventName.KeyPress || evt.type == XEventName.KeyRelease)
                HandleKeyEvent(evt.KeyEvent);
            else if (evt.type == XEventName.PropertyNotify && (evt.PropertyEvent.atom == atomGrab || evt.PropertyEvent.atom == atomUngrab))
                HandleInvokeAtom(evt.PropertyEvent.atom);
        }

        private void HandleKeyEvent(XKeyEvent evt)
        {
            bool pressed = evt.type == XEventName.KeyPress;

            WindowsVirtualKey key = KeyTranslator.LinuxToWindows((LinuxKeyCode)evt.keycode);

            if (pressed)
                OnInputReceived(new ISInputData(ISInputCode.IS_KEYDOWN, (short)key, 0));
            else
                OnInputReceived(new ISInputData(ISInputCode.IS_KEYUP, (short)key, 0));
        }

        private void HandleInvokeAtom(IntPtr atom)
        {
            if (atom == atomGrab)
                GrabInput();
            else if (atom == atomUngrab)
                UngrabInput();
        }

        private int lastX = 0;
        private int lastY = 0;
        private int motionNum = 0;

        XEvent _evt = new XEvent();
        private void HandleMotionEvent(XMotionEvent evt)
        {
            if (_stopGrab)
            {
                return;
            }

            EventMask m = (EventMask)evt.state;
            CheckButtonStates(m);
            int relX = (evt.x_root - lastX);
            int relY = (evt.y_root - lastY);


            //We need to put the cursor back to the middle of the window, but we also need to make sure that we don't 
            //read the event
            XTestFakeMotionEvent(xConnection.XDisplay, 0, lastX, lastY, 0);

            _evt = new XEvent();
            _evt.type = XEventName.ClientMessage;
            _evt.ClientMessageEvent.ptr1 = atomIgnoreNext;
            _evt.ClientMessageEvent.format = 32;
            _evt.ClientMessageEvent.window = xWindow;
            XSendEvent(xConnection.XDisplay, xWindow, true, 0, ref _evt);

            XFlush(xConnection.XDisplay);

            OnInputReceived(new ISInputData(ISInputCode.IS_MOUSEMOVERELATIVE, (short)relX, (short)relY));
            motionNum++;
        }

        private bool PredicateCallback(IntPtr display, ref XEvent evt, IntPtr _)
        {
            return !evt.type.HasFlag(XEventName.MotionNotify);
        }

        private void CheckButtonStates(EventMask mask)
        {
            if (mask.HasFlag(EventMask.Button1MotionMask))
            {
                if (!buttonStates[1])
                {
                    buttonStates[1] = true;
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSELDOWN, 0, 0));
                }
            }
            else if (buttonStates[1])
            {
                buttonStates[1] = false;
                OnInputReceived(new ISInputData(ISInputCode.IS_MOUSELUP, 0, 0));
            }

            if (mask.HasFlag(EventMask.Button2MotionMask))
            {
                if (!buttonStates[2])
                {
                    buttonStates[2] = true;
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSEMDOWN, 0, 0));
                }
            }
            else if (buttonStates[2])
            {
                buttonStates[2] = false;
                OnInputReceived(new ISInputData(ISInputCode.IS_MOUSEMUP, 0, 0));
            }
            if (mask.HasFlag(EventMask.Button3MotionMask))
            {
                if (!buttonStates[3])
                {
                    buttonStates[3] = true;
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSERDOWN, 0, 0));
                }
            }
            else if (buttonStates[3])
            {
                buttonStates[3] = false;
                OnInputReceived(new ISInputData(ISInputCode.IS_MOUSERUP, 0, 0));
            }

        }


        private void OnInputReceived(ISInputData input)
        {
            InputReceived?.Invoke(this, input);
        }

        private void HandleButtonPressEvent(XButtonEvent evt)
        {
            buttonStates[evt.button] = true;

            switch (evt.button)
            {
                case 1:
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSELDOWN, 0, 0));
                    return;
                case 2:
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSEMDOWN, 0, 0));
                    return;
                case 3:
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSERDOWN, 0, 0));
                    return;
                case 8:
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSEXDOWN, 1, 0));
                    return;
                case 9:
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSEXDOWN, 2, 0));
                    return;
            }
        }

        private void HandleButtonReleaseEvent(XButtonEvent evt)
        {
            buttonStates[evt.button] = false;
            switch (evt.button)
            {
                case 1:
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSELUP, 0, 0));
                    return;
                case 2:
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSEMUP, 0, 0));
                    return;
                case 3:
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSERUP, 0, 0));
                    return;
                case 8:
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSEXUP, 1, 0));
                    return;
                case 9:
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSEXUP, 2, 0));
                    return;
            }
        }

        private void InvokeGrabInput(bool grab)
        {
            if (grab)
                XChangeProperty(xConnection.XDisplay, xWindow, atomGrab, new IntPtr(4), 32, 0, new byte[0], 1);
            else
                XChangeProperty(xConnection.XDisplay, xWindow, atomUngrab, new IntPtr(4), 32, 0, new byte[0], 1);

            XFlush(xConnection.XDisplay);
        }

        private void GrabInput()
        {
            if (InputBlocked)
                return;

            _stopGrab = false;

            XQueryPointer(xConnection.XDisplay, XDefaultRootWindow(xConnection.XDisplay), out _, out _, out storedX, out storedY, out _, out _, out int _);

            if (!EnsureVisible())
            {
                ISLogger.Write("Failed to get window input focus!");
                return;
            }

            int ret = XGrabKeyboard(xConnection.XDisplay, xWindow, true, 1, 1, IntPtr.Zero);

            if (ret != 0)
            {
                XUnmapWindow(xConnection.XDisplay, xWindow);
                ISLogger.Write("{0}: XGrabKeyboard returned {1}", ModuleName, ret);
                return;
            }

            ret = XGrabPointer(xConnection.XDisplay, xWindow, true, anyMotionMask, 1, 1, xWindow, IntPtr.Zero, IntPtr.Zero);

            if (ret != 0)
            {
                XUngrabPointer(xConnection.XDisplay, IntPtr.Zero);
                XUnmapWindow(xConnection.XDisplay, xWindow);
                ISLogger.Write("{0}: XGrabPointer returned {1}", ModuleName, ret);
                return;
            }

            XSelectInput(xConnection.XDisplay, xWindow, EventMask.PropertyChangeMask | anyMotionMask);
            XFlush(xConnection.XDisplay);

            XWarpPointer(xConnection.XDisplay, XDefaultRootWindow(xConnection.XDisplay), xWindow, 0, 0, 0, 0, 50, 50);
            XFlush(xConnection.XDisplay);
            XCheckMaskEvent(xConnection.XDisplay, EventMask.PointerMotionMask | EventMask.PointerMotionHintMask, out XEvent evtB);
            XQueryPointer(xConnection.XDisplay, xWindow, out _, out _, out lastX, out lastY, out _, out _, out _);
            InputBlocked = true;

            ISLogger.Write("{0}: Input grabbed", ModuleName);
            XFlush(xConnection.XDisplay);
        }

        private void UngrabInput()
        {
            if (!InputBlocked)
                return;
            _stopGrab = true;

            XUngrabKeyboard(xConnection.XDisplay, IntPtr.Zero);
            XFlush(xConnection.XDisplay);
            XUngrabPointer(xConnection.XDisplay, IntPtr.Zero);
            XFlush(xConnection.XDisplay);
            XUnmapWindow(xConnection.XDisplay, xWindow);
            ISLogger.Write("Restoring cursor to " + storedX + " + " + storedY);
            XWarpPointer(xConnection.XDisplay, IntPtr.Zero, XDefaultRootWindow(xConnection.XDisplay), 0, 0, 0, 0, storedX, storedY);
            XFlush(xConnection.XDisplay);
            InputBlocked = false;
            ISLogger.Write("{0}: Input ungrabbed", ModuleName);
        }

        private bool EnsureVisible()
        {

            ISLogger.Write("1");
            XMapRaised(xConnection.XDisplay, xWindow);

            for (int i = 0; i < 10; i++)
            {
                MapState state = GetMapState();

                if (state == MapState.IsViewable)
                {
                    ISLogger.Write("Window now viewable!");
                    return true;
                }


                ISLogger.Write("State = " + state);

                XMapRaised(xConnection.XDisplay, xWindow);
                XFlush(xConnection.XDisplay);
                Thread.Sleep(150);
            }

            return false;
        }

        private MapState GetMapState()
        {
            ISLogger.Write("Getting state");
            XGetWindowAttributes(xConnection.XDisplay, xWindow, out XWindowAttributes attribs);
            ISLogger.Write("Got state");
            return attribs.map_state;
        }
    }
}
