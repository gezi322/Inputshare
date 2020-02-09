using InputshareLib.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.PlatformModules.Input
{
    public class NullInputModule : InputModuleBase
    {
        public override bool InputRedirected { get; protected set; }
        public override Rectangle VirtualDisplayBounds { get ; protected set; }

        public override event EventHandler<SideHitArgs> SideHit;
        public override event EventHandler<InputData> InputReceived;
        public override event EventHandler<Rectangle> DisplayBoundsUpdated;

        public override void SetInputRedirected(bool redirect)
        {

        }

        public override void SetMouseHidden(bool hide)
        {

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
