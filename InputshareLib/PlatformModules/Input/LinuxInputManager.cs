﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InputshareLib.Clipboard;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Input;
using InputshareLib.Input.Hotkeys;
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

        private IntPtr atomGrab;
        private IntPtr atomUngrab;
        private IntPtr atomIgnoreNext;
        private int storedX = 0;
        private int storedY = 0;
        private bool[] buttonStates = new bool[16];

        private bool _stopGrab = false;

        private SharedXConnection xConnection;

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
            Init();
        }

        protected override void OnStop()
        {
            xConnection.EventArrived -= XConnection_EventArrived;
        }

        private void Init()
        {
            atomGrab = XInternAtom(xConnection.XDisplay, "ISGrab", false);
            atomUngrab = XInternAtom(xConnection.XDisplay, "ISUngrab", false);
            atomIgnoreNext = XInternAtom(xConnection.XDisplay, "ISIgnoreNext", false);

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

            if (evt.type == XEventName.MotionNotify && InputBlocked)
                HandleMotionEvent(evt.MotionEvent);
            if (evt.type == XEventName.ButtonPress && InputBlocked)
                HandleButtonPressEvent(evt.ButtonEvent);
            else if (evt.type == XEventName.ButtonRelease && InputBlocked)
                HandleButtonReleaseEvent(evt.ButtonEvent);
            else if (evt.type == XEventName.KeyPress || evt.type == XEventName.KeyRelease)
                HandleKeyEvent(evt.KeyEvent);
            else if (evt.type == XEventName.PropertyNotify && (evt.PropertyEvent.atom == atomGrab || evt.PropertyEvent.atom == atomUngrab))
                HandleInvokeAtom(evt.PropertyEvent.atom);
        }





        private void HandleKeyEvent(XKeyEvent evt)
        {
            bool pressed = evt.type == XEventName.KeyPress;

            if (Settings.DEBUG_PRINTINPUTKEYS)
                ISLogger.Write("DEBUG: KEY {0} (raw {1})", (LinuxKeyCode)evt.keycode, evt.keycode);

            //Raise a hotkey pressed event instead of an input event
            if (pressed)
                if (CheckForHotkey(evt))
                    return;

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
            SendIgnoreNext();
            XFlush(xConnection.XDisplay);

            OnInputReceived(new ISInputData(ISInputCode.IS_MOUSEMOVERELATIVE, (short)relX, (short)relY));
        }

        private void SendIgnoreNext()
        {
            _evt = new XEvent();
            _evt.type = XEventName.ClientMessage;
            _evt.ClientMessageEvent.ptr1 = atomIgnoreNext;
            _evt.ClientMessageEvent.format = 32;
            _evt.ClientMessageEvent.window = xConnection.XRootWindow;

            XSendEvent(xConnection.XDisplay, xConnection.XRootWindow, true, 0, ref _evt);
        }


        private EventMask[] buttonMasks = new EventMask[] {0, EventMask.Button1MotionMask, EventMask.Button2MotionMask, EventMask.Button3MotionMask,
        EventMask.Button4MotionMask, EventMask.Button5MotionMask};

        private ISInputCode[] buttonMaskInputCodes = new ISInputCode[] { 0, ISInputCode.IS_MOUSELDOWN, ISInputCode.IS_MOUSELDOWN, ISInputCode.IS_MOUSEMDOWN }; 

        private void CheckButtonStates(EventMask mask)
        {
            for (int i = 1; i < 4; i++)
            {
                if (mask.HasFlag(buttonMasks[i]))
                {
                    if (!buttonStates[i])
                    {
                        buttonStates[i] = true;
                        OnInputReceived(new ISInputData(buttonMaskInputCodes[i], 0, 0));
                    }
                } else if (buttonStates[i])
                {
                    buttonStates[i] = false;
                    OnInputReceived(new ISInputData(buttonMaskInputCodes[i], 0, 0));
                }
            }
        }


        private void OnInputReceived(ISInputData input)
        {
            InputReceived?.Invoke(this, input);
        }

        protected override void OnHotkeyAdded(Hotkey key)
        {
            LinuxKeyMask mask = ConvertMask(key.Modifiers);
            LinuxKeyCode lKey = KeyTranslator.WindowsToLinux(key.Key);

            //We don't care if the num lock or scroll lock is on/off, so we need 
            //to grab those seperate to ensure that we receive events regardless of the state of scroll/num lock

            XGrabKey(xConnection.XDisplay, lKey, mask,
                xConnection.XRootWindow, true, 1, 1);

            XGrabKey(xConnection.XDisplay, lKey, mask | LinuxKeyMask.NumLockMask,
               xConnection.XRootWindow, true, 1, 1);

            XGrabKey(xConnection.XDisplay, lKey, mask | LinuxKeyMask.ScrollLockMask,
              xConnection.XRootWindow, true, 1, 1);
        }

        protected override void OnHotkeyRemoved(Hotkey key)
        {
            LinuxKeyMask mask = ConvertMask(key.Modifiers);
            LinuxKeyCode lKey = KeyTranslator.WindowsToLinux(key.Key);

            XUngrabKey(xConnection.XDisplay, lKey, mask,
                xConnection.XRootWindow);

            XUngrabKey(xConnection.XDisplay, lKey, mask | LinuxKeyMask.NumLockMask,
               xConnection.XRootWindow);

            XUngrabKey(xConnection.XDisplay, lKey, mask | LinuxKeyMask.ScrollLockMask,
               xConnection.XRootWindow);
        }

        private bool CheckForHotkey(XKeyEvent evt)
        {
            if (evt.type != XEventName.KeyPress)
                return false;

            foreach(var hk in hotkeys)
            {
                if(hk.Key == KeyTranslator.LinuxToWindows((LinuxKeyCode)evt.keycode) 
                    && ((LinuxKeyMask)evt.state).HasFlag(ConvertMask(hk.Modifiers))){
                    if (hk is FunctionHotkey fhk)
                        OnFunctionHotkeyPressed(fhk.Function);
                    else if (hk is ClientHotkey chk)
                        OnClientHotkeyPressed(chk.TargetClient);

                    return true;
                }
            }

            return false;
        }

        private LinuxKeyMask ConvertMask(HotkeyModifiers mods)
        {
            LinuxKeyMask mask = 0;

            if (mods.HasFlag(HotkeyModifiers.Alt))
                mask |= LinuxKeyMask.AltMask;
            if (mods.HasFlag(HotkeyModifiers.Ctrl))
                mask |= LinuxKeyMask.ControlMask;
            if (mods.HasFlag(HotkeyModifiers.Shift))
                mask |= LinuxKeyMask.ShiftMask;
            if (mods.HasFlag(HotkeyModifiers.Windows))
                mask |= LinuxKeyMask.WindowsMask;

            return mask;
        }

        //TODO - button 4 & 5 for scroll
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
                case 4:
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSEYSCROLL, 120, 0));
                    return;
                case 5:
                    OnInputReceived(new ISInputData(ISInputCode.IS_MOUSEYSCROLL, -120, 0));
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
            XEvent evt = new XEvent();
            evt.type = XEventName.PropertyNotify;
            evt.AnyEvent.window = xConnection.XRootWindow;

            if (grab)
                evt.PropertyEvent.atom = atomGrab;
            else
                evt.PropertyEvent.atom = atomUngrab;

            xConnection.QueueLocalEvent(evt);
            XFlush(xConnection.XDisplay);
        }

        private void GrabInput()
        {
            if (InputBlocked)
                return;

            _stopGrab = false;
            XQueryPointer(xConnection.XDisplay, xConnection.XRootWindow, out _, out _, out storedX, out storedY, out _, out _, out int _);

            XWarpPointer(xConnection.XDisplay, xConnection.XRootWindow, xConnection.XRootWindow, 0, 0, 0, 0, 200, 200);
            XFlush(xConnection.XDisplay);
            XQueryPointer(xConnection.XDisplay, xConnection.XRootWindow, out _, out _, out lastX, out lastY, out _, out _, out _);

            int ret = XGrabKeyboard(xConnection.XDisplay, xConnection.XRootWindow, true, 1, 1, IntPtr.Zero);

            if (ret != 0)
            {

                ISLogger.Write("{0}: XGrabKeyboard returned {1}", ModuleName, ret);
                return;
            }

            ret = XGrabPointer(xConnection.XDisplay, xConnection.XRootWindow, true, anyMotionMask, 1, 1, xConnection.XRootWindow, IntPtr.Zero, IntPtr.Zero);

            if (ret != 0)
            {
                XUngrabPointer(xConnection.XDisplay, IntPtr.Zero);
                ISLogger.Write("{0}: XGrabPointer returned {1}", ModuleName, ret);
                return;
            }

            XFlush(xConnection.XDisplay);

            
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
            ISLogger.Write("Restoring cursor to " + storedX + " + " + storedY);
            XWarpPointer(xConnection.XDisplay, IntPtr.Zero, XDefaultRootWindow(xConnection.XDisplay), 0, 0, 0, 0, storedX, storedY);
            XFlush(xConnection.XDisplay);
            InputBlocked = false;
            ISLogger.Write("{0}: Input ungrabbed", ModuleName);
        }


    }
}
