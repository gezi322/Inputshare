using InputshareLib.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Inputshare.Models
{
    internal sealed class ISClientModel
    {
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        private ISClient clientInstance;

        public bool IsConnected { get => clientInstance.IsConnected; }
        public IPEndPoint ConnectedAddress { get; private set; }
        public string ClientName { get => clientInstance.ClientName; }

        internal ISClientModel()
        {
            clientInstance = new ISClient();
            clientInstance.Connected += ClientInstance_Connected;
            clientInstance.ConnectionError += ClientInstance_ConnectionError;
            clientInstance.ConnectionFailed += ClientInstance_ConnectionFailed;
            clientInstance.Disconnected += ClientInstance_Disconnected;

            Disconnected += ISClientModel_Disconnected;
        }

        private void ISClientModel_Disconnected(object sender, EventArgs e)
        {
            if (clientInstance.Running && !clientInstance.AutoReconnect)
                clientInstance.Stop();
        }

        public void Connect(ISClientStartOptionsModel options, IPEndPoint address, string userName)
        {
            List<string> o = new List<string>();
            if (!options.EnableClipboard)
                o.Add("noclipboard");
            if (!options.EnableDragDrop)
                o.Add("nodragdrop");
            if (!options.EnableUdp)
                o.Add("noudp");
            if (options.AutoReconnect)
                o.Add("Autoreconnect");

            if(!clientInstance.Running)
                clientInstance.Start(new InputshareLib.StartOptions(o), GetPlatformDependencies());

            clientInstance.ClientName = userName;
            ConnectedAddress = address;
            clientInstance.Connect(address);
        }

        public void Disconnect()
        {
            if (clientInstance.IsConnected)
            {
                clientInstance.Disconnect();
            }
        }

        public void StopIfRunning()
        {
            if (clientInstance.Running)
                clientInstance.Stop();
        }

        private void ClientInstance_Disconnected(object sender, EventArgs e)
        {
            Disconnected?.Invoke(this, null);
        }

        private void ClientInstance_ConnectionFailed(object sender, string e)
        {
            Disconnected?.Invoke(this, null);
        }

        private void ClientInstance_ConnectionError(object sender, string e)
        {
            Disconnected?.Invoke(this, null);
        }

        private void ClientInstance_Connected(object sender, System.Net.IPEndPoint addr)
        {
            Connected?.Invoke(this, null);
        }

        private ISClientDependencies GetPlatformDependencies()
        {
#if WindowsBuild
            return InputshareLibWindows.WindowsDependencies.GetClientDependencies();
#elif LinuxBuild
            return ISClientDependencies.GetLinuxDependencies();
#endif
        }
    }
}
