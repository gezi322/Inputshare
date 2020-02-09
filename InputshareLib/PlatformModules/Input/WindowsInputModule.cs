using InputshareLib.Input;
using InputshareLib.PlatformModules.Windows;
using InputshareLib.PlatformModules.Windows.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static InputshareLib.PlatformModules.Windows.Native.User32;

namespace InputshareLib.PlatformModules.Input
{
    public class WindowsInputModule : InputModuleBase
    {
        public override bool InputRedirected { get; protected set; }
        public override Rectangle VirtualDisplayBounds { get; protected set; }
        public override event EventHandler<InputData> InputReceived;
        public override event EventHandler<SideHitArgs> SideHit;
        public override event EventHandler<Rectangle> DisplayBoundsUpdated;

        private WinMessageWindow _window;
        private IntPtr _mHook;
        private IntPtr _kbHook;
        private HookCallback _mCallback;
        private HookCallback _kbCallback;
        private bool _hidingMouse;

        public WindowsInputModule()
        {
            _mCallback = MouseCallback;
            _kbCallback = KeyboardCallback;
        }

        public override void SetInputRedirected(bool redirect)
        {
            InputRedirected = redirect;
            GetCursorPos(out _oldPos);

            
        }

        /// <summary>
        /// Show the window under the cursor to hide the cursor
        /// </summary>
        private void HideMouse()
        {
            
            _window.InvokeAction(() => {
                if (!SetWindowPos(_window.Handle, new IntPtr(-1), _oldPos.X-50, _oldPos.Y-50, 100, 100, 0x0040 | 0x0010))
                    throw new Win32Exception();

                _hidingMouse = true;
            });
        }
        private void ShowMouse()
        {
            
            _window.InvokeAction(() => {
                ShowWindow(_window.Handle, 0);
                _hidingMouse = false;
            });
        }
        protected override async Task OnStart()
        {
            UpdateVirtualDisplayBounds();
            _window = await WinMessageWindow.CreateWindowAsync("IS_InputWnd");
            _window.MessageRecevied += OnWindowMessageReceived;
            InstallHooks(_window);
        }

        protected override Task OnStop()
        {
            _window.Dispose();

            return Task.CompletedTask;
        }

        private void OnWindowMessageReceived(object sender, Win32Message e)
        {
            Win32MessageCode code = (Win32MessageCode)e.message;

            //Use WM_DISPLAYCHANGE messages as an event that the virtual display bounds have changed
            if (code == Win32MessageCode.WM_DISPLAYCHANGE)
                UpdateVirtualDisplayBounds();
        }

        private void UpdateVirtualDisplayBounds()
        {
            //TODO
            VirtualDisplayBounds = new Rectangle(GetSystemMetrics(76), GetSystemMetrics(77),
                GetSystemMetrics(78), GetSystemMetrics(79));

            Logger.Write($"Display bounds:  {VirtualDisplayBounds.Width}:{VirtualDisplayBounds.Height}");
            DisplayBoundsUpdated?.Invoke(this, VirtualDisplayBounds);
        }

        /// <summary>
        /// Installs mouse & keyboard hooks onto the specified window
        /// </summary>
        /// <param name="window"></param>
        private void InstallHooks(WinMessageWindow window)
        {
            window.InvokeAction(() => {
                //To hide the mouse, we move a window under the cursor and call the ShowCursor(false) method.
                //Here we set the opacity of the window to 1
                SetWindowLongPtr(_window.Handle, GWL_EXSTYLE, new IntPtr(WS_EX_LAYERED));
                SetLayeredWindowAttributes(_window.Handle, 0, 1, LWA_ALPHA);
                ShowCursor(false);
                //Set dpi awareness to GetCursorPos() gives cursor position ignoring DPI-scaling
                SetProcessDpiAwareness(new IntPtr(2));

                _mHook = SetWindowsHookEx(WH_MOUSE_LL, _mCallback, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
                if (_mHook == default)
                    throw new Win32Exception();
                _kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbCallback, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
                if (_kbHook == default)
                    throw new Win32Exception();
            });
        }

        private POINT _oldPos;
        private MSLLHOOKSTRUCT _mouseStruct = new MSLLHOOKSTRUCT();
        private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            //Get the MSLLHOOKSTRUCT from lparam
            Marshal.PtrToStructure(lParam, _mouseStruct);

            //check if cursor position is at the edge of the virtual screen
            if ((_mouseStruct.pt.X != _oldPos.X || _mouseStruct.pt.Y != _oldPos.Y))
            {
                if (VirtualDisplayBounds.Left >= _mouseStruct.pt.X)
                    SideHit?.Invoke(this, new SideHitArgs(Side.Left, _mouseStruct.pt.X, _mouseStruct.pt.Y));
                else if (VirtualDisplayBounds.Right - 2 < _mouseStruct.pt.X)
                    SideHit?.Invoke(this, new SideHitArgs(Side.Right, _mouseStruct.pt.X, _mouseStruct.pt.Y));
                else if (VirtualDisplayBounds.Top >= _mouseStruct.pt.Y)
                    SideHit?.Invoke(this, new SideHitArgs(Side.Top, _mouseStruct.pt.X, _mouseStruct.pt.Y));
                else if (VirtualDisplayBounds.Bottom - 2 < _mouseStruct.pt.Y)
                    SideHit?.Invoke(this, new SideHitArgs(Side.Bottom, _mouseStruct.pt.X, _mouseStruct.pt.Y));
            }

            if (InputRedirected && ((_mouseStruct.flags & 4) == 0))
            {
                //If we are redirecting input, translate the input to generic input data and fire event
                Win32MessageCode code = (Win32MessageCode)wParam;
                if (code == Win32MessageCode.WM_MOUSEMOVE)
                    InputReceived?.Invoke(this, WindowsInputTranslator.WindowsMouseMoveToGeneric(ref _mouseStruct, _oldPos.X, _oldPos.Y));
                else
                    InputReceived?.Invoke(this, WindowsInputTranslator.WindowsToGeneric((Win32MessageCode)wParam, ref _mouseStruct));

                //returning -1 here will prevent system from sending the input to any other programs/hooks
                return new IntPtr(-1);
            }
            else
            {
 
                return IntPtr.Zero;
            }
        }

        private KBDLLHOOKSTRUCT _kbStruct = new KBDLLHOOKSTRUCT();
        private IntPtr KeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Marshal.PtrToStructure(lParam, _kbStruct);

            if (InputRedirected && ((_mouseStruct.flags & 4) == 0))
            {
                InputReceived?.Invoke(this, WindowsInputTranslator.WindowsToGeneric((Win32MessageCode)wParam, ref _kbStruct));
                return new IntPtr(-1);
            }
            else
            {
                return IntPtr.Zero;
            }
            
        }
        
        /// <summary>
        /// Shows or hides the cursor. Cursor must be frozen
        /// </summary>
        /// <param name="hide"></param>
        public override void SetMouseHidden(bool hide)
        {
            GetCursorPos(out _oldPos);
            if (!hide && hide != _hidingMouse)
                ShowMouse();
            else
                HideMouse();
        }
    }
}
