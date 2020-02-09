using InputshareLib.Input;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace InputshareLib.Server.Displays
{
    /// <summary>
    /// Represents the server' display
    /// </summary>
    public class LocalDisplay : DisplayBase
    {
        private InputModuleBase _inputModule;
        private OutputModuleBase _outputModule;

        public LocalDisplay(InputModuleBase inputModule, OutputModuleBase outputModule) : base(inputModule.VirtualDisplayBounds, "Localhost")
        {
            _inputModule = inputModule;
            _inputModule.DisplayBoundsUpdated += _inputModule_DisplayBoundsUpdated;
            _inputModule.SideHit += (object o, SideHitArgs args) => { OnSideHit(args.Side, args.PosX, args.PosY); };
            _outputModule = outputModule;
        }

        private void _inputModule_DisplayBoundsUpdated(object sender, Rectangle bounds)
        {
            OnDisplayBoundsChanged(bounds);
        }

        public override void SendInput(ref InputData input)
        {
            _outputModule.SimulateInput(ref input);
        }

        public override void SetInputDisplay(int newX, int newY)
        {
            var input = new InputData(InputCode.MouseMoveAbsolute, (short)newX, (short)newY);
            _outputModule.SimulateInput(ref input);
            _inputModule.SetMouseHidden(false);
            _inputModule.SetInputRedirected(false);
            InputActive = true;
        }

        public override void SetNotInputDisplay()
        {
            _inputModule.SetInputRedirected(true);
            _inputModule.SetMouseHidden(true);
        }
    }
}
