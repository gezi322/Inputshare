#if WindowsBuild
using InputshareLib;
using InputshareLibWindows.IPC.NetIpc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Models
{
    internal class ISWinServiceModel
    {
        public event EventHandler<string> ServiceConnectionLost;
        public bool ServiceConnected { get; private set; }

        public event EventHandler Connected;
        public event EventHandler Disconnected;

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
                service.ServiceLogMessage += Service_ServiceLogMessage;
                service.ServerConnected += Connected;
                service.ServerDisconnected += Disconnected;
                ISLogger.Write("Connected to service");
                ServiceConnected = true;
                return true;
            }catch(Exception ex)
            {
                ISLogger.Write("Failed to connect to service: " + ex.Message);
                return false;
            }
        }

        private void Service_ServiceLogMessage(object sender, string msg)
        {
            ISLogger.Write("Service: " + msg);
        }

        public void DisconnectService()
        {
            if (ServiceConnected)
            {
                service.Dispose();
                ServiceConnected = false;
            }
        }

        public void ClientConnect(IPEndPoint address)
        {
            service.Connect(address);
        }

        private void Service_Disconnected(object sender, string e)
        {
            ISLogger.Write("Lost connection to service: " + e);
            ServiceConnected = false;
            ServiceConnectionLost?.Invoke(this, e);
        }

        public async Task<bool> GetConnectedStateAsync()
        {
            return await service.GetConnectedStateAsync();
        }
        
        public async Task<string> GetNameAsync()
        {
            return await service.GetClientNameAsync();
        }

        public async Task<IPEndPoint> GetAddressAsync()
        {
            return await service.GetConnectedAddressAsync();
        }

        public void SetName(string clientName)
        {
            service.SetName(clientName);
        }

        public void ClientDisconnect()
        {
            service.Disconnect();
        }
    }
}
#endif