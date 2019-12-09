using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using InputshareLib;
using InputshareLib.Client;
using ReactiveUI;

namespace Inputshare.ViewModels
{
    public class ClientViewModel : ViewModelBase
    {
        private ISClient clientInstance;
        public bool EnableClipboardChecked { get; set; } = true;
        public bool EnableDragDropChecked { get; set; } = true;

        public string ClientName { get; set; } = Environment.MachineName;

        public ClientViewModel(ISClient client)
        {
            clientInstance = client;

            CommandConnect = ReactiveCommand.Create(ExecConnect);
        }

        private string portEntryText = "4441";
        private string addressEntryText = "192.168.0.12";
        public string PortEntryText { get { return portEntryText; } set { OnPortEntryTextChanged(value); } }
        public string AddressEntryText { get { return addressEntryText; } set { OnAddressEntryTextChange(value); } }
        public ReactiveCommand CommandConnect { get; }
        
        private void OnPortEntryTextChanged(string text)
        {
            portEntryText = text;
        }
        private void OnAddressEntryTextChange(string text)
        {
            addressEntryText = text;
        }

        private void ExecConnect()
        {
            if (!IPAddress.TryParse(addressEntryText, out IPAddress addr))
                return;

            if (!int.TryParse(portEntryText, out int port))
                return;


            List<string> options = new List<string>();
            if (!EnableDragDropChecked)
                options.Add("NoDragDrop");
            if (!EnableClipboardChecked)
                options.Add("NoClipboard");

            clientInstance.ClientName = ClientName;

            if (!clientInstance.Running)
            {
#if WindowsBuild
            clientInstance.Start(new StartOptions(options), InputshareLibWindows.WindowsDependencies.GetClientDependencies());
#else
                clientInstance.Start(new StartOptions(options), ISClientDependencies.GetLinuxDependencies());
#endif
            }


            clientInstance.Connect(new IPEndPoint(addr, port));
        }
    }
}
