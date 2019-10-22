using InputshareLib.Cursor;
using InputshareLibWindows.IPC.AnonIpc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace InputshareLibWindows.Cursor
{
    public class ServiceCursorMonitor : CursorMonitorBase
    {
        private IpcHandle host;

        public ServiceCursorMonitor(IpcHandle mainHost)
        {
            host = mainHost;
            host.host.EdgeHit += MainHost_EdgeHit;
            host.HandleUpdated += Host_HandleUpdated;
        }

        private void Host_HandleUpdated(object sender, EventArgs e)
        {
            host.host.EdgeHit += MainHost_EdgeHit;
        }

        private void MainHost_EdgeHit(object sender, InputshareLib.Edge edge)
        {
            HandleEdgeHit(edge);
        }

        

        public override void StartMonitoring(Rectangle bounds)
        {

        }

        public override void StopMonitoring()
        {

        }
    }
}
