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

namespace Inputshare.Common.PlatformModules.Output
{
    public class X11OutputModule : OutputModuleBase
    {
        private IntPtr _xDisplay;

        internal X11OutputModule(XConnection connection)
        {
            _xDisplay = connection.XDisplay;
        }

        public override void SimulateInput(ref InputData input)
        {
            switch (input.Code)
            {
                case InputCode.MouseMoveRelative:
                    XWarpPointer(_xDisplay, IntPtr.Zero, IntPtr.Zero, 0, 0, 0, 0, input.ParamA, input.ParamB);
                    break;
                case InputCode.Mouse1Down:
                    XTestFakeButtonEvent(_xDisplay, X11_LEFTBUTTON, true, 0);
                    break;
                case InputCode.Mouse1Up:
                    XTestFakeButtonEvent(_xDisplay, X11_LEFTBUTTON, false, 0);
                    break;
            }

            XFlush(_xDisplay);
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
