using InputshareLib;
using InputshareLib.PlatformModules.Cursor;
using System;
using System.Drawing;
using System.Threading;

using static InputshareLibWindows.Native.User32;

namespace InputshareLibWindows.PlatformModules.Cursor
{
    public class WindowsCursorMonitor : CursorMonitorBase
    {
        private Timer monitorTimer;

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

        protected override void OnStart()
        {
            virtualDisplayBounds = new Rectangle(0, 0, 1024, 768);
            monitorTimer = new Timer(MonitorTimerCallback, 0, 0, 50);
        }

        protected override void OnStop()
        {
            monitorTimer?.Dispose();
        }
    }
}
