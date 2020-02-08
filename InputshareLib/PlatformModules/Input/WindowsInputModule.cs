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

        public override event EventHandler<InputData> InputReceived;
        public override event EventHandler<Side> SideHit;

        private WinMessageWindow _window;
        private IntPtr _mHook;
        private IntPtr _kbHook;
        private Rectangle _virtualDisplayBounds;

        public override void SetInputRedirected(bool redirect)
        {
            GetCursorPos(out _oldPos);
            InputRedirected = redirect;
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
            _virtualDisplayBounds = new Rectangle(GetSystemMetrics(76), GetSystemMetrics(77),
                GetSystemMetrics(78), GetSystemMetrics(79));

            Logger.Write($"Display bounds:  {_virtualDisplayBounds.Width}:{_virtualDisplayBounds.Height}");
        }

        /// <summary>
        /// Installs mouse & keyboard hooks onto the specified window
        /// </summary>
        /// <param name="window"></param>
        private void InstallHooks(WinMessageWindow window)
        {
            window.InvokeAction(() => {
                _mHook = SetWindowsHookEx(WH_MOUSE_LL, MouseCallback, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
                if (_mHook == default)
                    throw new Win32Exception();
                _kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardCallback, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
                if (_kbHook == default)
                    throw new Win32Exception();
            });
        }

        private POINT _oldPos;
        private MSLLHOOKSTRUCT _mouseStruct = new MSLLHOOKSTRUCT();
        private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Marshal.PtrToStructure(lParam, _mouseStruct);

            if ((_mouseStruct.pt.X != _oldPos.X || _mouseStruct.pt.Y != _oldPos.Y))
            {
                if (_virtualDisplayBounds.Left >= _mouseStruct.pt.X)
                    SideHit?.Invoke(this, Side.Left);
                else if (_virtualDisplayBounds.Right - 2 < _mouseStruct.pt.X)
                    SideHit?.Invoke(this, Side.Right);
                else if (_virtualDisplayBounds.Top >= _mouseStruct.pt.Y)
                    SideHit?.Invoke(this, Side.Top);
                else if (_virtualDisplayBounds.Bottom - 2 < _mouseStruct.pt.Y)
                    SideHit?.Invoke(this, Side.Bottom);
            }

            if (InputRedirected)
            {
                Win32MessageCode code = (Win32MessageCode)wParam;
                if (code == Win32MessageCode.WM_MOUSEMOVE)
                    InputReceived?.Invoke(this, WindowsInputTranslator.WindowsMouseMoveToGeneric(ref _mouseStruct, _oldPos.X, _oldPos.Y));
                else
                    InputReceived?.Invoke(this, WindowsInputTranslator.WindowsToGeneric((Win32MessageCode)wParam, ref _mouseStruct));
                
                User32.GetCursorPos(out _oldPos);
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

            if (InputRedirected)
            {
                InputReceived?.Invoke(this, WindowsInputTranslator.WindowsToGeneric((Win32MessageCode)wParam, ref _kbStruct));
                return new IntPtr(-1);
            }
            else
            {
                return IntPtr.Zero;
            }
            
        }
    }
}
