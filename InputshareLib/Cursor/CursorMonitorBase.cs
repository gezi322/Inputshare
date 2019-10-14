using System;
using System.Diagnostics;
using System.Drawing;

namespace InputshareLib.Cursor
{
    public abstract class CursorMonitorBase
    {
        public event EventHandler<Edge> EdgeHit;
        protected Rectangle virtualDisplayBounds;
        public bool Running { get; protected set; }
        public abstract void StartMonitoring(Rectangle bounds);
        public abstract void StopMonitoring();

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
