using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Clipboard.DataTypes;

namespace InputshareLib.Input
{
    public class NullInputManager : InputManagerBase
    {
        public override bool LeftMouseDown => throw new NotImplementedException();

        public override bool InputBlocked { get; protected set; }
        public override bool Running { get; protected set; }
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

        public override void Stop()
        {
            Running = false;
        }
    }
}
