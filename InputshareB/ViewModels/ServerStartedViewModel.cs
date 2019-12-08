using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using InputshareLib.Server;
using InputshareLib.Server.API;
using ReactiveUI;

namespace InputshareB.ViewModels
{
    public class ServerStartedViewModel : ViewModelBase
    {
        private ISServer serverInstance;
        private ObservableCollection<ClientInfo> clients = new ObservableCollection<ClientInfo>();

        public string CurrentInputClientText { get => "Current input client: " + CurrentInputClient.Name; }
        private ClientInfo CurrentInputClient = ClientInfo.None;

        public ServerStartedViewModel(ISServer server)
        {
            CommandStopServer = ReactiveCommand.Create(ExecStopServer);
            serverInstance = server;

            server.ClientConnected += Server_ClientConnected;
            server.ClientDisconnected += Server_ClientDisconnected;
            server.InputClientSwitched += Server_InputClientSwitched;
        }

        private void Server_InputClientSwitched(object sender, InputshareLib.Server.API.ClientInfo client)
        {
            UpdateCurrentInputClient(client);
        }

        private void Server_ClientDisconnected(object sender, InputshareLib.Server.API.ClientInfo client)
        {
            clients.Remove(client);
        }

        private void Server_ClientConnected(object sender, InputshareLib.Server.API.ClientInfo client)
        {
            clients.Add(client);
            serverInstance.SetClientEdge(client, InputshareLib.Edge.Left, serverInstance.GetLocalhost());
        }

        private void UpdateCurrentInputClient(ClientInfo client)
        {
            CurrentInputClient = client;
            this.RaisePropertyChanged(nameof(CurrentInputClientText));
        }

        public ReactiveCommand CommandStopServer { get; }

        private void ExecStopServer()
        {
            serverInstance.Stop();
        }
    }
}
