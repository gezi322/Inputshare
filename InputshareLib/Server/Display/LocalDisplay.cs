using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using InputshareLib.Input;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;

namespace InputshareLib.Server.Display
{
    public class LocalDisplay : DisplayBase
    {
        private InputModuleBase _inputModule;
        private OutputModuleBase _outputModule;

        internal LocalDisplay(InputModuleBase inputModule, OutputModuleBase outputModule) : base(inputModule.VirtualDisplayBounds, "Localhost")
        {
            _inputModule = inputModule;
            _outputModule = outputModule;

            _inputModule.SideHit += (object o, SideHitArgs args) => base.OnSideHit(args.Side, args.PosX, args.PosY);
        }

        protected override void SendSideChanged()
        {

        }

        internal override void SendInput(ref InputData input)
        {
           _outputModule.SimulateInput(ref input);
        }

        internal override void SetInputActive()
        {
            _inputModule.SetInputRedirected(false);
            _inputModule.SetMouseHidden(false);
        }

        internal override void SetInputInactive()
        {
            _inputModule.SetInputRedirected(true);
            _inputModule.SetMouseHidden(true);
        }
    }
}
