using InputshareLib.Linux;
using System.Drawing;
using System.Threading;
using static InputshareLib.Linux.Native.LibX11;

namespace InputshareLib.PlatformModules.Cursor
{
    public class LinuxCursorMonitor : CursorMonitorBase
    {
        private SharedXConnection xConnection;
        private Timer positionTimer;

        public LinuxCursorMonitor(SharedXConnection xCon)
        {
            virtualDisplayBounds = new Rectangle(0, 0, 1024, 768);
            xConnection = xCon;
        }

        protected override void OnStart()
        {
            positionTimer = new Timer(positionTimerCallback, null, 0, 50);
        }

        protected override void OnStop()
        {
            positionTimer?.Dispose();
        }

        private void positionTimerCallback(object sync)
        {
            XQueryPointer(xConnection.XDisplay, xConnection.XRootWindow, out _, out _, out int posX, out int posY, out _, out _, out int keys);
            if (posY == virtualDisplayBounds.Bottom - 1)
                HandleEdgeHit(Edge.Bottom);
            if (posY == virtualDisplayBounds.Top)
                HandleEdgeHit(Edge.Top);
            if (posX == virtualDisplayBounds.Left)
                HandleEdgeHit(Edge.Left);
            if (posX == virtualDisplayBounds.Right - 1)
                HandleEdgeHit(Edge.Right);

        }
    }
}
