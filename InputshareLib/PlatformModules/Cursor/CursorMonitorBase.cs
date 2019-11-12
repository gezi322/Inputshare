using System;
using System.Diagnostics;
using System.Drawing;

namespace InputshareLib.PlatformModules.Cursor
{
    public abstract class CursorMonitorBase : PlatformModuleBase
    {
        public event EventHandler<Edge> EdgeHit;
        protected Rectangle virtualDisplayBounds;

        private Stopwatch monitorStopwatch = new Stopwatch();

        public CursorMonitorBase()
        {
            monitorStopwatch.Start();
        }

        public virtual void SetBounds(Rectangle bounds)
        {
            virtualDisplayBounds = bounds;
        }

        protected virtual void HandleEdgeHit(Edge edge)
        {
            if (monitorStopwatch.ElapsedMilliseconds > 200)
            {
                EdgeHit?.Invoke(this, edge);
                monitorStopwatch.Restart();
            }
        }
    }
}
