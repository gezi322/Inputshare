﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using Avalonia.Input;
using InputshareLib;
using InputshareLib.Input.Hotkeys;
using InputshareLib.Input.Keys;
using InputshareLib.Server;
using InputshareLib.Server.API;
using ReactiveUI;
using Key = Avalonia.Input.Key;
using KeyEventArgs = Avalonia.Input.KeyEventArgs;

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

        private bool clientSettingsShiftChecked = false;
        public bool ClientSettingsShiftChecked { get { return clientSettingsShiftChecked; } private set { this.RaiseAndSetIfChanged(ref clientSettingsShiftChecked, value); } }
        private bool clientSettingsAltChecked = false;
        public bool ClientSettingsAltChecked { get { return clientSettingsAltChecked; } private set { this.RaiseAndSetIfChanged(ref clientSettingsAltChecked, value); } }
        private bool clientSettingsCtrlChecked = false;
        public bool ClientSettingsCtrlChecked { get { return clientSettingsCtrlChecked; } private set { this.RaiseAndSetIfChanged(ref clientSettingsCtrlChecked, value); } }

        private bool hotkeyEntering = false;
        public ReactiveCommand ClientSettingsHotkeyCommand { get; }

        public ServerStartedViewModel(ISServer server)
        {
            server.Started += Server_Started;
            server.Stopped += Server_Stopped;

            CommandStopServer = ReactiveCommand.Create(ExecStopServer);
            ClientSettingsHotkeyCommand = ReactiveCommand.Create(ExecHotkeyCommand);

            serverInstance = server;
            server.ClientConnected += Server_ClientConnected;
            server.ClientDisconnected += Server_ClientDisconnected;
            server.InputClientSwitched += Server_InputClientSwitched;
            server.ClientInfoUpdated += Server_ClientInfoUpdated;
        }

        public void ExecHotkeyCommand()
        {
            hotkeyEntering = !hotkeyEntering;
            ISLogger.Write("Hotkey entering = " + hotkeyEntering);
        }

        public void HandleKeyDown(KeyEventArgs args)
        {
            if (!hotkeyEntering)
                return;

            //ignore modifier keys
            if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl
                || args.Key == Key.RightAlt || args.Key == Key.LeftAlt
                || args.Key == Key.LeftShift || args.Key == Key.RightShift)
                return;

            ClientSettingsHotkeyButtonText = args.Key.ToString();
            hotkeyEntering = false;
            
            SetEnteredHotkey(args.Key);
        }

        private void SetEnteredHotkey(Key key)
        {
            try
            {
                HotkeyModifiers mods = 0;
                if (ClientSettingsAltChecked)
                    mods |= HotkeyModifiers.Alt;
                if (ClientSettingsShiftChecked)
                    mods |= HotkeyModifiers.Shift;
                if (ClientSettingsCtrlChecked)
                    mods |= HotkeyModifiers.Ctrl;

#if WindowsBuild
                System.Windows.Input.Key a = (System.Windows.Input.Key)key;
                Hotkey k = new Hotkey((WindowsVirtualKey)KeyInterop.VirtualKeyFromKey(a), mods);
#else
                //Translate from avalonia key to windows virtual key
                //This is a dirty method but should work for the majority of keys
                var a = (WindowsVirtualKey)Enum.Parse(typeof(WindowsVirtualKey), key.ToString());
                Hotkey k = new Hotkey(a, mods);
#endif

                serverInstance.SetHotkeyForClient(selectedClient, k);
            }catch(Exception ex)
            {
                ISLogger.Write("Failed to set client hotkey: " + ex.Message);
                ClientSettingsHotkeyButtonText = "Error";
            }
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
            {
                
                ClientSettingsHotkeyButtonText = selectedClient.ClientHotkey.Key.ToString();
                ClientSettingsAltChecked = selectedClient.ClientHotkey.Modifiers.HasFlag(HotkeyModifiers.Alt);
                ClientSettingsCtrlChecked = selectedClient.ClientHotkey.Modifiers.HasFlag(HotkeyModifiers.Ctrl);
                ClientSettingsShiftChecked = selectedClient.ClientHotkey.Modifiers.HasFlag(HotkeyModifiers.Shift);
            }
            else
            {
                ClientSettingsAltChecked = false;
                ClientSettingsCtrlChecked = false;
                ClientSettingsShiftChecked = false;
                ClientSettingsHotkeyButtonText = "None";
            }
               

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
