#if WindowsBuild
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using InputshareLib;
using InputshareLibWindows.IPC.NetIpc;
using ReactiveUI;

namespace Inputshare.ViewModels
{
    public class WindowsServiceViewModel : ViewModelBase
    {
        private bool connectToServiceVisible = true;
        public bool ConnectToServiceVisible { get { return connectToServiceVisible; } private set { this.RaiseAndSetIfChanged(ref connectToServiceVisible, value); } }

        private bool connectGridVisible = false;
        public bool ConnectGridVisible { get { return connectGridVisible; } private set { this.RaiseAndSetIfChanged(ref connectGridVisible, value); } }
        private bool disconnectGridVisible = false;
        public bool DisconnectGridVisible { get { return disconnectGridVisible; } private set { this.RaiseAndSetIfChanged(ref disconnectGridVisible, value); } }

        public ReactiveCommand CommandConnectService { get; }
        public ReactiveCommand CommandConnect { get; }
        public ReactiveCommand CommandDisconnect { get; }
        private NetIpcClient client;


        private string portEntryText = "4441";
        private string addressEntryText = "192.168.0.12";
        public string PortEntryText { get { return portEntryText; } set { OnPortEntryTextChanged(value); } }
        public string AddressEntryText { get { return addressEntryText; } set { OnAddressEntryTextChange(value); } }
        public string ClientName { get; set; } = Environment.MachineName;

        public WindowsServiceViewModel()
        {
            CommandConnectService = ReactiveCommand.CreateFromTask(ExecConnectService);
            CommandConnect = ReactiveCommand.Create(ExecConnect);
            CommandDisconnect = ReactiveCommand.Create(ExecDisconnect);
        }
       
        private void ExecConnect()
        {
            if (!IPAddress.TryParse(addressEntryText, out _))
                return;

            if (!int.TryParse(portEntryText, out int port))
                return;

            client.SetName(ClientName);
            client.Connect(new IPEndPoint(IPAddress.Parse(addressEntryText), port));
        }

        private void ExecDisconnect()
        {
            client.Disconnect();
        }

        private void OnPortEntryTextChanged(string text)
        {
            portEntryText = text;
        }
        private void OnAddressEntryTextChange(string text)
        {
            addressEntryText = text;
        }

        private async Task ExecConnectService()
        {
            await Task.Run(() => {
                try
                {
                    client = new NetIpcClient("ServiceIpc");
                }catch(Exception ex)
                {
                    ISLogger.Write("Could not connect to service: " + ex.Message);
                    return;
                }
                
                client.Disconnected += Client_Disconnected;
                client.ServerConnected += Client_ServerConnected;
                client.ServerDisconnected += Client_ServerDisconnected;
                ConnectToServiceVisible = false;
                bool connected = client.GetConnectedState().Result;

                ConnectGridVisible = !connected;
                DisconnectGridVisible = connected;
            });
        }

        private void Client_Disconnected(object sender, string e)
        {
            DisconnectGridVisible = false;
            ConnectGridVisible = false;
            ConnectToServiceVisible = true;
        }

        private void Client_ServerDisconnected(object sender, EventArgs e)
        {
            ConnectGridVisible = true;
            DisconnectGridVisible = false;
        }

        private void Client_ServerConnected(object sender, EventArgs e)
        {
            ConnectGridVisible = false;
            DisconnectGridVisible = true;
        }
    }
}



#else
using System;
using System.Collections.Generic;
using System.Text;


namespace Inputshare.ViewModels
{
    public class WindowsServiceViewModel : ViewModelBase
    {
        public WindowsServiceViewModel()
        {

        }
    }
}
#endif
