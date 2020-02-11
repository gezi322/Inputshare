using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using InputshareLib.Input;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;

namespace InputshareLib.Server.Display
{
    public class LocalDisplay : DisplayBase
    {
        private readonly InputModuleBase _inputModule;
        private readonly OutputModuleBase _outputModule;

        internal LocalDisplay(InputModuleBase inputModule, OutputModuleBase outputModule) : base(inputModule.VirtualDisplayBounds, "Localhost")
        {
            _inputModule = inputModule;
            _outputModule = outputModule;

            _inputModule.SideHit += (object o, SideHitArgs args) => base.OnSideHit(args.Side, args.PosX, args.PosY);
        }

        protected override Task SendSideChangedAsync()
        {
            return Task.CompletedTask;
        }

        internal override void SendInput(ref InputData input)
        {
           _outputModule.SimulateInput(ref input);
        }

        internal override Task NotfyInputActiveAsync()
        {
            _inputModule.SetInputRedirected(false);
            _inputModule.SetMouseHidden(false);
            return Task.CompletedTask;
        }

        internal override Task NotifyClientInvactiveAsync()
        {
            _inputModule.SetInputRedirected(true);
            _inputModule.SetMouseHidden(true);
            return Task.CompletedTask;
        }
    }
}
