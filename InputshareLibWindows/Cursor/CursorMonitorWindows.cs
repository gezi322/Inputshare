using InputshareLib;
using System;
using System.Drawing;
using System.Threading;

using static InputshareLibWindows.Native.User32;

namespace InputshareLibWindows.Cursor
{
    public class WindowsCursorMonitor : InputshareLib.Cursor.CursorMonitorBase
    {
        private Timer monitorTimer;

        public override void StartMonitoring(Rectangle bounds)
        {
            if (Running)
                throw new InvalidOperationException("Already monitoring");

            Running = true;
            virtualDisplayBounds = bounds;
            monitorTimer = new Timer(MonitorTimerCallback, 0, 0, 50);
            //ISLogger.Write("Windows cursor monitor started");
        }

        private void MonitorTimerCallback(object state)
        {
            GetCursorPos(out POINT ptn);

            if (ptn.X == virtualDisplayBounds.Left)
                HandleEdgeHit(Edge.Left);
            else if (ptn.X == virtualDisplayBounds.Right - 1)
                HandleEdgeHit(Edge.Right);
            else if (ptn.Y == virtualDisplayBounds.Top)
                HandleEdgeHit(Edge.Top);
            else if (ptn.Y == virtualDisplayBounds.Bottom - 1)
                HandleEdgeHit(Edge.Bottom);
        }

        public override void StopMonitoring()
        {
            if (!Running)
                throw new InvalidOperationException("Already stopped");

            monitorTimer?.Dispose();
            Running = false;
            //ISLogger.Write("Windows cursor monitor stopped");
        }
    }
}
