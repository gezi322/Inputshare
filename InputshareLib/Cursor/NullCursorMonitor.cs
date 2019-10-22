using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace InputshareLib.Cursor
{
    public class NullCursorMonitor : CursorMonitorBase
    {
        public override void StartMonitoring(Rectangle bounds)
        {
            Running = true;
        }

        public override void StopMonitoring()
        {
            Running = false;
        }
    }
}
