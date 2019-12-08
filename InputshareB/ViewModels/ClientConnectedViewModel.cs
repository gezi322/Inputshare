using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Client;
using ReactiveUI;

namespace InputshareB.ViewModels
{
    public class ClientConnectedViewModel : ViewModelBase
    {
        ISClient clientInstance;
        public string ConnectedToText { get; private set; }
        public string ClientName { get; private set; }

        public ReactiveCommand CommandDisconnect { get; private set; }

        public ClientConnectedViewModel(ISClient client)
        {
            clientInstance = client;
            client.Connected += Client_Connected;
            CommandDisconnect = ReactiveCommand.Create(ExecDisconnect);
        }

        private void ExecDisconnect()
        {
            clientInstance.Disconnect();
        }

        private void Client_Connected(object sender, System.Net.IPEndPoint e)
        {
            ConnectedToText = "Connected to: " + e;
            this.RaisePropertyChanged(nameof(ConnectedToText));

            ClientName = clientInstance.ClientName;
            this.RaisePropertyChanged(nameof(ClientName));
        }
    }
}
