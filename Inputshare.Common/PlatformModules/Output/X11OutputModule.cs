using Inputshare.Common.Input;
using Inputshare.Common.PlatformModules.Linux;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Inputshare.Common.PlatformModules.Linux.Native.LibXtst;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11;
using System.Threading;
using System.Collections.Concurrent;
using Inputshare.Common.Input.Keys;

namespace Inputshare.Common.PlatformModules.Output
{
    public class X11OutputModule : OutputModuleBase
    {
        private IntPtr _xDisplay;

        /// <summary>
        /// Creates an instance of the X11 output module with the given Xdisplay connection
        /// </summary>
        /// <param name="connection"></param>
        internal X11OutputModule(XConnection connection)
        {
            _xDisplay = connection._xDisplay;
        }

        public override void SimulateInput(ref InputData input)
        {
            if (input.Code == InputCode.MouseMoveRelative)
                XWarpPointer(_xDisplay, IntPtr.Zero, IntPtr.Zero, 0, 0, 0, 0, input.ParamA, input.ParamB);
            else if (input.Code == InputCode.Mouse1Down)
                XTestFakeButtonEvent(_xDisplay, X11_LEFTBUTTON, true, 0);
            else if (input.Code == InputCode.Mouse1Up)
                XTestFakeButtonEvent(_xDisplay, X11_LEFTBUTTON, false, 0);
            else if (input.Code == InputCode.Mouse2Down)
                XTestFakeButtonEvent(_xDisplay, X11_RIGHTBUTTON, true, 0);
            else if (input.Code == InputCode.Mouse2Up)
                XTestFakeButtonEvent(_xDisplay, X11_RIGHTBUTTON, false, 0);
            else if (input.Code == InputCode.MouseMDown)
                XTestFakeButtonEvent(_xDisplay, X11_MIDDLEBUTTON, true, 0);
            else if (input.Code == InputCode.MouseMUp)
                XTestFakeButtonEvent(_xDisplay, X11_MIDDLEBUTTON, false, 0);
            else if (input.Code == InputCode.MouseYScroll)
                HandleYScroll(input.ParamA);
            else if (input.Code == InputCode.MouseXDown)
                XTestFakeButtonEvent(_xDisplay, input.ParamA == 4 ? X11_XBUTTONFORWARD : X11_XBUTTONBACK, true, 0);
            else if (input.Code == InputCode.MouseXUp)
                XTestFakeButtonEvent(_xDisplay, input.ParamA == 4 ? X11_XBUTTONFORWARD : X11_XBUTTONBACK, false, 0);
            else if (input.Code == InputCode.KeyDownVKey)
                PressKey(input.ParamA, true);
            else if (input.Code == InputCode.KeyUpVKey)
                PressKey(input.ParamA, false);
            else if (input.Code == InputCode.KeyDownScan)
                Logger.Warning("KeyDownScan not implemented");
            else if (input.Code == InputCode.keyUpScan)
                Logger.Warning("KeyUpScan not implemented");

            XFlush(_xDisplay);
        }

        private void HandleYScroll(short direction)
        {
            if (direction > 0)
            {
                XTestFakeButtonEvent(_xDisplay, X11_SCROLLDOWN, true, 0);
                XTestFakeButtonEvent(_xDisplay, X11_SCROLLDOWN, false, 0);
            }
            else
            {
                XTestFakeButtonEvent(_xDisplay, X11_SCROLLUP, true, 0);
                XTestFakeButtonEvent(_xDisplay, X11_SCROLLUP, false, 0);
            }
        }

        private void PressKey(short key, bool press)
        {
            LinuxKeyCode translatedKey = KeyTranslator.WindowsToLinux((WindowsVirtualKey)key);

            if (translatedKey != LinuxKeyCode.None)
                XTestFakeKeyEvent(_xDisplay, (uint)translatedKey, press, 0);
        }

        protected override Task OnStart()
        {
            return Task.CompletedTask;
        }

        protected override Task OnStop()
        {
            return Task.CompletedTask;
        }
    }
}
