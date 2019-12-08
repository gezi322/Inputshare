using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using InputshareLib;
using InputshareLib.Server;
using InputshareLib.Server.API;
using ReactiveUI;

namespace InputshareB.ViewModels
{
    public class ServerStartedViewModel : ViewModelBase
    {
        private ISServer serverInstance;

        public AsyncObservableCollection<ClientInfo> ClientListItems { get; } = new AsyncObservableCollection<ClientInfo>();
        public AsyncObservableCollection<ClientInfo> ClientListExcludeSelected { get; } = new AsyncObservableCollection<ClientInfo>();

        public string CurrentInputClientText { get => "Current input client: " + CurrentInputClient.Name; }
        private ClientInfo CurrentInputClient = ClientInfo.None;

        private bool clientSettingsVisible;
        public bool ClientSettingsVisible { get { return clientSettingsVisible; } private set { this.RaiseAndSetIfChanged(ref clientSettingsVisible, value); } }

        private string clientSettingsHeaderText;
        public string ClientSettingsHeaderText { get { return clientSettingsHeaderText; } private set { this.RaiseAndSetIfChanged(ref clientSettingsHeaderText, value); } }

        private ClientInfo selectedClient = ClientInfo.None;
        public ClientInfo SelectedClient { get { return selectedClient; } set { selectedClient = value; OnSelectedClientChanged(); } }

        private string clientSettingsAddressText;
        public string ClientSettingsAddressText { get { return clientSettingsAddressText; } private set { this.RaiseAndSetIfChanged(ref clientSettingsAddressText, value); } }
        private string clientSettingsHotkeyButtonText;
        public string ClientSettingsHotkeyButtonText { get { return clientSettingsHotkeyButtonText; } private set { this.RaiseAndSetIfChanged(ref clientSettingsHotkeyButtonText, value); } }
        private string clientSettingsDisplayText;
        public string ClientSettingsDisplayText { get { return clientSettingsDisplayText; } private set { this.RaiseAndSetIfChanged(ref clientSettingsDisplayText, value); } }

        private ClientInfo clientSettingsLeftClient = ClientInfo.None;
        public ClientInfo ClientSettingsLeftClient { get { return clientSettingsLeftClient; } set { clientSettingsLeftClient = value; OnClientEdgeChanged(Edge.Left, value); } }

        private ClientInfo clientSettingsRightClient = ClientInfo.None;
        public ClientInfo ClientSettingsRightClient { get { return clientSettingsRightClient; } set { clientSettingsRightClient = value; OnClientEdgeChanged(Edge.Right, value); } }
        
        private ClientInfo clientSettingsTopClient = ClientInfo.None;
        public ClientInfo ClientSettingsTopClient { get { return clientSettingsTopClient; } set { clientSettingsTopClient = value; OnClientEdgeChanged(Edge.Top, value); } }
        private ClientInfo clientSettingsBottomClient = ClientInfo.None;
        public ClientInfo ClientSettingsBottomClient { get { return clientSettingsBottomClient; } set { clientSettingsBottomClient = value; OnClientEdgeChanged(Edge.Bottom, value); } }


        public ServerStartedViewModel(ISServer server)
        {
            server.Started += Server_Started;
            server.Stopped += Server_Stopped;

            CommandStopServer = ReactiveCommand.Create(ExecStopServer);
            serverInstance = server;
            server.ClientConnected += Server_ClientConnected;
            server.ClientDisconnected += Server_ClientDisconnected;
            server.InputClientSwitched += Server_InputClientSwitched;
            server.ClientInfoUpdated += Server_ClientInfoUpdated;
        }

        private void Server_ClientInfoUpdated(object sender, ClientInfo client)
        {
            ClientListItems.Clear();
            foreach(var c in serverInstance.GetAllClients())
            {
                ClientListItems.Add(c);
            }

            BuildExcludedClientList();
        }

        private void OnSelectedClientChanged()
        {
            if (selectedClient == null)
            {
                ClientSettingsVisible = false;
                return;
            }

            BuildExcludedClientList();
            ClientSettingsVisible = true;
            ClientSettingsHeaderText = "Settings for " + selectedClient.Name;
            ClientSettingsAddressText = "Client address: " + selectedClient.ClientAddress;

            if (selectedClient.ClientHotkey != null)
                ClientSettingsHotkeyButtonText = selectedClient.ClientHotkey.ToString();
            else
                ClientSettingsHotkeyButtonText = "None";

            ClientSettingsDisplayText = "Display bounds: " + selectedClient.DisplayConf.VirtualBounds;
            clientSettingsLeftClient = selectedClient.LeftClient;
            clientSettingsRightClient = selectedClient.RightClient;
            clientSettingsTopClient = selectedClient.TopClient;
            clientSettingsBottomClient = selectedClient.BottomClient;
            this.RaisePropertyChanged(nameof(ClientSettingsLeftClient));
            this.RaisePropertyChanged(nameof(ClientSettingsRightClient));
            this.RaisePropertyChanged(nameof(ClientSettingsTopClient));
            this.RaisePropertyChanged(nameof(ClientSettingsBottomClient));

        }

        private void BuildExcludedClientList()
        {
            ClientListExcludeSelected.Clear();
            ClientListExcludeSelected.Add(ClientInfo.None);
            foreach(var client in ClientListItems)
            {
                if (client == selectedClient)
                    continue;

                ClientListExcludeSelected.Add(client);
            }
        }

        private void OnClientEdgeChanged(Edge edge, ClientInfo client)
        {
            if (selectedClient == null || selectedClient == ClientInfo.None || client == null)
                return;

            serverInstance.SetClientEdge(client, edge, selectedClient);
        }

        private void Server_Stopped(object sender, EventArgs e)
        {
            ClientListItems.Clear();
        }

        private void Server_Started(object sender, EventArgs e)
        {
            ClientListItems.Clear();
            ClientListItems.Add(serverInstance.GetLocalhost());
        }

        private void Server_InputClientSwitched(object sender, InputshareLib.Server.API.ClientInfo client)
        {
            UpdateCurrentInputClient(client);
        }

        private void Server_ClientDisconnected(object sender, InputshareLib.Server.API.ClientInfo client)
        {
            ClientListItems.Remove(client);
        }

        private void Server_ClientConnected(object sender, InputshareLib.Server.API.ClientInfo client)
        {
            ClientListItems.Add(client);
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
