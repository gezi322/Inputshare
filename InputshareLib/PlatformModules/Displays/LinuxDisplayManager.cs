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

        private byte[] currentRawConfig = new byte[0];
        private Timer displayUpdateTimer;

        public LinuxDisplayManager(SharedXConnection xCon)
        {
            xConnection = xCon;
        }

        protected override void OnStart()
        {
            displayUpdateTimer = new Timer(TimerCallback, null, 0, 1000);
        }

        protected override void OnStop()
        {
            displayUpdateTimer?.Dispose();
        }

        private void TimerCallback(object sync)
        {
            DisplayConfig conf = GetXDisplayConfig();

            if (!conf.ToBytes().SequenceEqual(CurrentConfig.ToBytes()))
            {
                CurrentConfig = conf;
                currentRawConfig = CurrentConfig.ToBytes();
                OnConfigUpdated(conf);
            }

        }

        private DisplayConfig GetXDisplayConfig()
        {
            XGetWindowAttributes(xConnection.XDisplay, xConnection.XRootWindow, out XWindowAttributes windowAttribs);
            //Todo - get display specific info
            return new DisplayConfig(new System.Drawing.Rectangle(windowAttribs.x, windowAttribs.y, windowAttribs.width, windowAttribs.height), new List<Display>());
        }

        public override void UpdateConfigManual()
        {
            CurrentConfig = GetXDisplayConfig();
            OnConfigUpdated(CurrentConfig);
        }
    }
}
