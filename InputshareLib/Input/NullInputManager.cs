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

        public override event EventHandler<ISInputData> InputReceived;
        public override event EventHandler<ClipboardDataBase> ClipboardDataChanged;

        public override void SetClipboardData(ClipboardDataBase cbData)
        {

        }

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
