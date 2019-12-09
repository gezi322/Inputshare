using InputshareLib.Input;
using System;

namespace InputshareLib.PlatformModules.Input
{
    public class NullInputManager : InputManagerBase
    {
        public override bool LeftMouseDown => false;

        public override bool InputBlocked { get; protected set; }
        public override MouseInputMode MouseRecordMode { get; protected set; }

#pragma warning disable CS0067
        public override event EventHandler<ISInputData> InputReceived;
#pragma warning restore CS0067

        public override void SetInputBlocked(bool block)
        {
        }

        public override void SetMouseInputMode(MouseInputMode mode, int interval = 0)
        {

        }

        protected override void OnStart()
        {

        }

        protected override void OnStop()
        {

        }
    }
}
