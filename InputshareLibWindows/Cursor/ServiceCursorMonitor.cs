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
        private AnonIpcHost host;

        public ServiceCursorMonitor(AnonIpcHost mainHost)
        {
            host = mainHost;
            mainHost.EdgeHit += MainHost_EdgeHit;
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
