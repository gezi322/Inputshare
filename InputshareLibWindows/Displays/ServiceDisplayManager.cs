using InputshareLib.Displays;
using InputshareLibWindows.IPC.AnonIpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.Displays
{
    public sealed class ServiceDisplayManager : DisplayManagerBase
    {
        private AnonIpcHost host;

        public ServiceDisplayManager(AnonIpcHost hostMain)
        {
            host = hostMain;
            host.DisplayConfigUpdated += Host_DisplayConfigUpdated;
        }

        private void Host_DisplayConfigUpdated(object sender, DisplayConfig newConfig)
        {
            OnConfigUpdated(newConfig);
        }

        public override void StartMonitoring()
        {

        }

        public override void StopMonitoring()
        {

        }

        public override void UpdateConfigManual()
        {
            CurrentConfig = host.GetDisplayConfig();
        }
    }
}
