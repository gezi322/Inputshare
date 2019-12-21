#if WindowsBuild
using InputshareLib;
using InputshareLibWindows.IPC.NetIpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Models
{
    internal class ISWinServiceModel
    {
        public event EventHandler<string> ServiceConnectionLost;

        public bool ServiceConnected { get; private set; }

        private NetIpcClient service;

        public ISWinServiceModel()
        {

        }

        public bool ConnectService()
        {
            try
            {
                service = new NetIpcClient("ServiceIPC");
                service.Disconnected += Service_Disconnected;
                ISLogger.Write("Connected to service");
                ServiceConnected = true;
                return true;
            }catch(Exception ex)
            {
                ISLogger.Write("Failed to connect to service: " + ex.Message);
                return false;
            }
        }

        public void DisconnectService()
        {
            if (ServiceConnected)
            {
                service.Dispose();
                ServiceConnected = false;
            }
        }

        private void Service_Disconnected(object sender, string e)
        {
            ISLogger.Write("Lost connection to service: " + e);
            ServiceConnected = false;
            ServiceConnectionLost?.Invoke(this, e);
        }
    }
}
#endif