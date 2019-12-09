using InputshareLib.Displays;
using InputshareLib.Linux;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static InputshareLib.Linux.Native.LibX11;

namespace InputshareLib.PlatformModules.Displays
{
    public class LinuxDisplayManager : DisplayManagerBase
    {
        private SharedXConnection xConnection;
        private Timer displayUpdateTimer;
        private Timer positionUpdateTimer;

        public LinuxDisplayManager(SharedXConnection xCon)
        {
            xConnection = xCon;
        }

        protected override void OnStart()
        {
            UpdateConfigManual();
            displayUpdateTimer = new Timer(TimerCallback, null, 0, 1000);
            positionUpdateTimer = new Timer(PositionUpdateTimerCallback, null, 0, 50);
        }

        protected override void OnStop()
        {
            positionUpdateTimer?.Dispose();
            displayUpdateTimer?.Dispose();
        }

        private void TimerCallback(object sync)
        {
            DisplayConfig conf = GetXDisplayConfig();

            if (!conf.Equals(CurrentConfig))
                OnConfigUpdated(conf);
        }

        private void PositionUpdateTimerCallback(object sync)
        {
            XQueryPointer(xConnection.XDisplay, xConnection.XRootWindow, out _, out _, out int posX, out int posY, out _, out _, out int keys);
            if (posY == CurrentConfig.VirtualBounds.Bottom - 1)
                OnEdgeHit(Edge.Bottom);
            if (posY == CurrentConfig.VirtualBounds.Top)
                OnEdgeHit(Edge.Top);
            if (posX == CurrentConfig.VirtualBounds.Left)
                OnEdgeHit(Edge.Left);
            if (posX == CurrentConfig.VirtualBounds.Right - 1)
                OnEdgeHit(Edge.Right);
        }

        private DisplayConfig GetXDisplayConfig()
        {
            XGetWindowAttributes(xConnection.XDisplay, xConnection.XRootWindow, out XWindowAttributes windowAttribs);
            //Todo - get display specific info
            return new DisplayConfig(new System.Drawing.Rectangle(windowAttribs.x, windowAttribs.y, windowAttribs.width, windowAttribs.height), new List<Display>());
        }

        public override void UpdateConfigManual()
        {
            DisplayConfig conf = GetXDisplayConfig();
            OnConfigUpdated(conf);
        }
    }
}
