using InputshareLib;
using InputshareLib.Displays;
using InputshareLibWindows.IPC.AnonIpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.Displays
{
    public sealed class ServiceDisplayManager : DisplayManagerBase
    {
        private IpcHandle host;

        public ServiceDisplayManager(IpcHandle hostMain)
        {
            host = hostMain;
            host.host.DisplayConfigUpdated += Host_DisplayConfigUpdated;
            host.HandleUpdated += Host_HandleUpdated;
        }

        private void Host_HandleUpdated(object sender, EventArgs e)
        {
            host.host.DisplayConfigUpdated += Host_DisplayConfigUpdated;
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
            try
            {
                OnConfigUpdated(host.host.GetDisplayConfig().Result);
            }
            catch(Exception ex)
            {
                ISLogger.Write("ServiceDisplayManager: Failed to update display config: " + ex.Message);
            }
            
        }
    }
}
