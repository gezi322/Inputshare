﻿using InputshareLib;
using InputshareLib.Displays;
using InputshareLib.PlatformModules.Displays;
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
            host.host.EdgeHit += (object _, Edge e) => { OnEdgeHit(e); };
        }
        protected override void OnStart()
        {

        }

        protected override void OnStop()
        {

        }

        private void Host_HandleUpdated(object sender, EventArgs e)
        {
            host.host.DisplayConfigUpdated += Host_DisplayConfigUpdated;
            host.host.EdgeHit += (object _, Edge e) => { OnEdgeHit(e); };
        }

        private void Host_DisplayConfigUpdated(object sender, DisplayConfig newConfig)
        {
            OnConfigUpdated(newConfig);
        }

        public override void UpdateConfigManual()
        {
            try
            {
                if (!host.host.IsConnected)
                    throw new Exception("Ipc not connected");

                OnConfigUpdated(host.host.GetDisplayConfig().Result);
            }
            catch(Exception ex)
            {
                ISLogger.Write("ServiceDisplayManager: Failed to update display config: " + ex.Message);
            }
            
        }

       
    }
}
