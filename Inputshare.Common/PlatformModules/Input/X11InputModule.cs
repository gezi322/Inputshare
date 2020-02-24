using Inputshare.Common.Input;
using Inputshare.Common.Input.Hotkeys;
using Inputshare.Common.Input.Keys;
using Inputshare.Common.PlatformModules.Linux;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11Structs;


namespace Inputshare.Common.PlatformModules.Input
{
    public class X11InputModule : InputModuleBase
    {
        public override Rectangle VirtualDisplayBounds { get; protected set; } 
        public override bool InputRedirected { get; protected set; } = false;
        public override event EventHandler<Rectangle> DisplayBoundsUpdated;
        public override event EventHandler<SideHitArgs> SideHit;
        public override event EventHandler<InputData> InputReceived;

        private EventMask anyMotionMask = EventMask.Button1MotionMask | EventMask.Button2MotionMask | EventMask.Button3MotionMask | EventMask.Button4MotionMask
                | EventMask.Button5MotionMask | EventMask.ButtonMotionMask | EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask;

        private XConnection _connection;
        private IntPtr _xDisplay;
        private IntPtr _xRootWindow;

        private IntPtr _atomCaptureInput;
        private IntPtr _atomReleaseInput;
        private Timer _cursorPositionTimer;

        private int _storedPosX;
        private int _storedPosY;
        private int _lastX;
        private int _lastY;

        public X11InputModule(XConnection connection)
        {
            _connection = connection;
        }

        private void OnMessageReceived(object sender, XEvent evt)
        {
            Logger.Verbose("Got message type " + evt.type);

            switch (evt.type)
            {
                case XEventName.PropertyNotify:
                    HandlePropertyNotifyEvent(evt.PropertyEvent);
                    break;
                case XEventName.MotionNotify:
                    if(InputRedirected)
                        HandleMotionEvent(evt.MotionEvent);
                    break;
                case XEventName.ButtonPress:
                case XEventName.ButtonRelease:
                    HandleButtonEvent(evt.ButtonEvent);
                    break;
                case XEventName.KeyPress:
                case XEventName.KeyRelease:
                    HandleKeyEvent(evt.KeyEvent);
                    break;
                case XEventName.ConfigureNotify:
                    HandleConfigureEvent(evt.ConfigureEvent);
                    break;
            }
        }

        private void HandleConfigureEvent(XConfigureEvent evt)
        {
            SetDisplayBounds(new Rectangle(evt.x, evt.y, evt.width, evt.height));
        }

        private void SetDisplayBounds(Rectangle rect)
        {
            VirtualDisplayBounds = rect;
            DisplayBoundsUpdated?.Invoke(this, rect);
        }

        private void HandlePropertyNotifyEvent(XPropertyEvent evt)
        {
            if (evt.atom == _atomCaptureInput)
                GrabInput();
            else if (evt.atom == _atomReleaseInput)
                UngrabInput();
        }

        private void HandleButtonEvent(XButtonEvent evt)
        {
            bool press = evt.type == XEventName.ButtonPress;

            if (evt.button == 1)
                RaiseInputReceived(press ? InputCode.Mouse1Down : InputCode.Mouse1Up, 0, 0);
            else if (evt.button == 2)
                RaiseInputReceived(press ? InputCode.MouseMDown : InputCode.MouseMUp, 0, 0);
            else if (evt.button == 3)
                RaiseInputReceived(press ? InputCode.Mouse2Down : InputCode.Mouse2Up, 0, 0);
            else if (evt.button == 4)
                RaiseInputReceived(InputCode.MouseYScroll, 120, 0);
            else if (evt.button == 5)
                RaiseInputReceived(InputCode.MouseYScroll, -120, 0);
            else if (evt.button == 8)
                RaiseInputReceived(press ? InputCode.MouseXDown : InputCode.MouseXUp, 1, 0);
            else if (evt.button == 9)
                RaiseInputReceived(press ? InputCode.MouseXDown : InputCode.MouseXUp, 2, 0);
        }

        private void HandleKeyEvent(XKeyEvent evt)
        {
            bool pressed = evt.type == XEventName.KeyPress;

            WindowsVirtualKey key = KeyTranslator.LinuxToWindows((LinuxKeyCode)evt.keycode);
            KeyModifiers mods = ConvertMask((LinuxKeyMask)evt.state);

            if (key == WindowsVirtualKey.None)
                return;
            CheckHotkeys(key, mods);

            if (pressed)
                HandleKeyDown(key);
            else
                HandleKeyUp(key);

            RaiseInputReceived(pressed ? InputCode.KeyDownVKey : InputCode.KeyUpVKey, (short)key, 0);
        }


        private void RaiseInputReceived(InputCode code, short paramA, short paramB)
        {
            InputReceived?.Invoke(this, new InputData(code, paramA, paramB));
        }

        private XEvent _tempEvt;
        private void HandleMotionEvent(XMotionEvent evt)
        {
            //calculate the position by comparing the new position to the position of the previous event
            int relX = (evt.x_root - _lastX);
            int relY = (evt.y_root - _lastY);
            _lastX = evt.x_root;
            _lastY = evt.y_root;

            //If this event was sent by us, it means that we need to clear all motion events
            //from the x queue to make sure that we can move the cursor back to the frozen
            //position without registering it as a standard movement
            if (evt.send_event)
            {
                do
                {
                    //Clear all pointer motion events until we receive a message that we send
                    XMaskEvent(_xDisplay, EventMask.PointerMotionMask, out _tempEvt);
                } while (!_tempEvt.AnyEvent.send_event);
                return;
            }

            //Send the first marker that allows us to detect this specific xwarppointer event
            SendIgnoreNext();
            XWarpPointer(_xDisplay, _xRootWindow, _xRootWindow, 0, 0, 0, 0, 50, 50);
            //send the second marker to allow us to know that we have removed the event from the queue
            //and can continue processing events normally
            SendIgnoreNext();
            XFlush(_xDisplay);
            InputReceived?.Invoke(this, new InputData(InputCode.MouseMoveRelative, (short)relX, (short)relY));
        }

        private void SendIgnoreNext()
        {
            XEvent evt = new XEvent();
            evt.type = XEventName.MotionNotify;
            evt.AnyEvent.window = _xRootWindow;
            evt.AnyEvent.send_event = true;
            evt.MotionEvent.x_root = 50;
            evt.MotionEvent.y_root = 50;
            evt.AnyEvent.display = _xDisplay;
            XSendEvent(_xDisplay, _connection.XInvokeEventWindow, true, EventMask.PointerMotionMask, ref evt);
        }

        protected override void OnHotkeyAdded(Hotkey hk)
        {
            LinuxKeyMask mask = ConvertMask(hk.Modifiers);
            LinuxKeyCode lKey = KeyTranslator.WindowsToLinux(hk.Key);

            XGrabKey(_xDisplay, lKey, mask,
                _xRootWindow, true, 1, 1);

            XGrabKey(_xDisplay, lKey, mask | LinuxKeyMask.NumLockMask,
              _xRootWindow, true, 1, 1);

            XGrabKey(_xDisplay, lKey, mask | LinuxKeyMask.ScrollLockMask,
              _xRootWindow, true, 1, 1);

            Logger.Debug($"{ModuleName}: Grabbed key {lKey} with modifiers {mask}");
        }

        protected override void OnHotkeyRemoved(Hotkey hk)
        {
            LinuxKeyMask mask = ConvertMask(hk.Modifiers);
            LinuxKeyCode lKey = KeyTranslator.WindowsToLinux(hk.Key);

            XUngrabKey(_xDisplay, lKey, mask,
               _xRootWindow);

            XUngrabKey(_xDisplay, lKey, mask | LinuxKeyMask.NumLockMask,
               _xRootWindow);

            XUngrabKey(_xDisplay, lKey, mask | LinuxKeyMask.ScrollLockMask,
               _xRootWindow);

            Logger.Debug($"{ModuleName}: Ungrabbed key {lKey} with modifiers {mask}");
        }

        private LinuxKeyMask ConvertMask(KeyModifiers mods)
        {
            LinuxKeyMask mask = 0;

            if (mods.HasFlag(KeyModifiers.Alt))
                mask |= LinuxKeyMask.AltMask;
            if (mods.HasFlag(KeyModifiers.Ctrl))
                mask |= LinuxKeyMask.ControlMask;
            if (mods.HasFlag(KeyModifiers.Shift))
                mask |= LinuxKeyMask.ShiftMask;
            if (mods.HasFlag(KeyModifiers.Win))
                mask |= LinuxKeyMask.WindowsMask;

            Logger.Debug($"{ModuleName}: Converted inputshare key modifiers {mods} to {mask}");
            return mask;
        }

        private KeyModifiers ConvertMask(LinuxKeyMask mask)
        {
            KeyModifiers mods = 0;

            if (mask.HasFlag(LinuxKeyMask.AltMask))
                mods |= KeyModifiers.Alt;
            if (mask.HasFlag(LinuxKeyMask.ControlMask))
                mods |= KeyModifiers.Ctrl;
            if (mask.HasFlag(LinuxKeyMask.ShiftMask))
                mods |= KeyModifiers.Shift;
            if (mask.HasFlag(LinuxKeyMask.WindowsMask))
                mods |= KeyModifiers.Win;

            Logger.Debug($"{ModuleName}: Converted linux key modifiers {mask} to {mods}");
            return mods;
        }

        private void GrabInput()
        {
            if (InputRedirected)
            {
                Logger.Warning($"{ModuleName}: Ignoring invoked grabinput: Input is already redirected");
                return;
            }

            Logger.Debug($"{ModuleName}: Grabbing input");

            //Store the positon so that we can return the cursor to the correct position
            //after input is ungrabbed
            XQueryPointer(_xDisplay, _xRootWindow, out _, out _, out _storedPosX, out _storedPosY, out _, out _, out int _);
            
            //Move the cursor to position 50,50 so that it can move in all directions
            XWarpPointer(_xDisplay, _xRootWindow, _xRootWindow, 0, 0, 0, 0, 50, 50);

            int ret = XGrabKeyboard(_xDisplay, _xRootWindow, false, 1, 1, IntPtr.Zero);
            if(ret != 0)
            {
                Logger.Error($"{ModuleName}: XGrabKeyboard failed! return value: {ret}");
                XWarpPointer(_xDisplay, _xRootWindow, _xRootWindow, 0, 0, 0, 0, _storedPosX, _storedPosX);
                return;
            }
            ret = XGrabPointer(_xDisplay, _xRootWindow, false, anyMotionMask, 1, 1, _xRootWindow, IntPtr.Zero, IntPtr.Zero);
            if (ret != 0)
            {
                Logger.Error($"{ModuleName}: XGrabPointer failed! return value: {ret}");
                XUngrabKeyboard(_xDisplay, IntPtr.Zero);
                XWarpPointer(_xDisplay, _xRootWindow, _xRootWindow, 0, 0, 0, 0, _storedPosX, _storedPosX);
                return;
            }

            XFlush(_xDisplay);
            InputRedirected = true;
            Logger.Debug($"{ModuleName}: Mouse & Keyboard grabbed");
        }

        private void UngrabInput()
        {
            if (!InputRedirected)
            {
                Logger.Warning($"{ModuleName}: Ignored invoke ungrabinput: Input is not redirected");
                return;
            }

            Logger.Debug($"{ModuleName}: Ungrabbing input");
            XUngrabKeyboard(_xDisplay, IntPtr.Zero);
            XUngrabPointer(_xDisplay, IntPtr.Zero);
            XFlush(_xDisplay);
            XWarpPointer(_xDisplay, IntPtr.Zero, XDefaultRootWindow(_xDisplay), 0, 0, 0, 0, _storedPosX, _storedPosY);
            XFlush(_xDisplay);
            InputRedirected = false;
            Logger.Debug($"{ModuleName}: Mouse & Keyboard ungrabbed");
        }

        protected override Task OnStart()
        {
            _xDisplay = _connection._xDisplay;
            _xRootWindow = XDefaultRootWindow(_xDisplay);

            _atomCaptureInput = XInternAtom(_xDisplay, nameof(_atomCaptureInput), false);
            _atomReleaseInput = XInternAtom(_xDisplay, nameof(_atomReleaseInput), false);
            _connection.EventReceived += OnMessageReceived;
            VirtualDisplayBounds = GetDisplayBounds();

            _cursorPositionTimer = new Timer(CursorPositionTimerCallback, null, 0, 50);

            return base.OnStart();
        }

        private void CursorPositionTimerCallback(object sync)
        {
            XQueryPointer(_xDisplay, _xRootWindow, out _, out _, out int posX, out int posY, out _, out _, out int keys);
            if (posY > VirtualDisplayBounds.Bottom - 2)
                SideHit?.Invoke(this, new SideHitArgs(Side.Bottom, posX, posY));
            else if (posY == VirtualDisplayBounds.Top)
                SideHit?.Invoke(this, new SideHitArgs(Side.Top, posX, posY));
            else if (posX == VirtualDisplayBounds.Left)
                SideHit?.Invoke(this, new SideHitArgs(Side.Left, posX, posY));
            else if (posX > VirtualDisplayBounds.Right - 2)
                SideHit?.Invoke(this, new SideHitArgs(Side.Right, posX, posY));
        }

        private Rectangle GetDisplayBounds()
        {
            XGetWindowAttributes(_xDisplay, _xRootWindow, out XWindowAttributes attribs);
            return new Rectangle(attribs.x, attribs.y, attribs.width, attribs.height);
        }

        protected override Task OnStop()
        {
            _cursorPositionTimer?.Dispose();
            _connection.EventReceived -= OnMessageReceived;
            return Task.CompletedTask;
        }

        public override void SetInputRedirected(bool redirect)
        {
            InvokeGrabInput(redirect);
        }

        public override void SetMouseHidden(bool hide)
        {
            Logger.Warning($"{ModuleName}: Ignoring setmousehidden: Not implemented");
        }

        private void InvokeGrabInput(bool grab)
        {
            XEvent evt = new XEvent();
            evt.type = XEventName.PropertyNotify;
            evt.AnyEvent.window = _xRootWindow;
            evt.PropertyEvent.atom = grab ? _atomCaptureInput : _atomReleaseInput;

            XSendEvent(_xDisplay, _connection.XInvokeEventWindow, false, EventMask.PropertyChangeMask, ref evt);
            Logger.Information($"{ModuleName}: Sent invoke grab input");
        }
        
    }
}
