using System;
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
using ReactiveUI;
using Key = Avalonia.Input.Key;
using KeyEventArgs = Avalonia.Input.KeyEventArgs;

namespace Inputshare.ViewModels
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

        public List<Hotkeyfunction> HotkeyFunctionList { get; } = new List<Hotkeyfunction>();

        public ServerStartedViewModel(ISServer server)
        {
            server.Started += Server_Started;
            server.Stopped += Server_Stopped;

            CommandStopServer = ReactiveCommand.Create(ExecStopServer);
            ClientSettingsHotkeyCommand = ReactiveCommand.Create(ExecHotkeyCommand);
            HotkeyEnterCommand = ReactiveCommand.Create(ExecHotkeyEnter);
            serverInstance = server;
            server.ClientConnected += Server_ClientConnected;
            server.InputClientSwitched += Server_InputClientSwitched;
            server.ClientDisconnected += Server_ClientDisconnected;
            BuildFunctionList();
        }

        private void Server_ClientDisconnected(object sender, ClientInfo e)
        {
            UpdateClientList();
        }

        private void BuildFunctionList()
        {
            HotkeyFunctionList.Clear();
            foreach(var func in Enum.GetValues(typeof(Hotkeyfunction)))
            {
                HotkeyFunctionList.Add((Hotkeyfunction)func);
            }
        }

        public void ExecHotkeyCommand()
        {
            hotkeyEntering = !hotkeyEntering;
        }

        public void HandleKeyDown(KeyEventArgs args)
        {
            if (!hotkeyEntering)
            {
                HandleKeyDownFunction(args);
                return;
            }

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

                SelectedClient.SetHotkey(k);
                ClientSettingsHotkeyButtonText = SelectedClient.ClientHotkey.Key.ToString();
            }
            catch(Exception ex)
            {
                ISLogger.Write("Failed to set client hotkey: " + ex.Message);
                ClientSettingsHotkeyButtonText = "Error";
            }
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

        private object listLock = new object();
        private void UpdateClientList()
        {
            Guid oldSelected = Guid.Empty;

            if (SelectedClient != null)
                oldSelected = selectedClient.Id;

            lock (listLock)
            {
                ClientListItems.Clear();
                foreach (var client in serverInstance.GetAllClients())
                {
                    ClientListItems.Add(client);
                }
            }
            

            foreach (var c in ClientListItems)
                if (c.Id == oldSelected)
                    SelectedClient = c;
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

            SelectedClient.SetEdge(edge, client);
            UpdateClientList();
        }

        private void Server_Stopped(object sender, EventArgs e)
        {
            ClientListItems.Clear();
        }

        private void Server_Started(object sender, EventArgs e)
        {
            ClientListItems.Clear();
            UpdateClientList();
        }

        private void Server_InputClientSwitched(object sender, ClientInfo client)
        {
            UpdateCurrentInputClient(client);
        }

        private void Server_ClientConnected(object sender, ClientInfo client)
        {
            UpdateClientList();
            client.PropertyChanged += Client_PropertyChanged;
        }

        private void Client_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateClientList();
        }

        private void Client_Disconnected(object sender, string e)
        {
            UpdateClientList();
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

        #region func Hotkeys
        private Hotkeyfunction selectedHotkeyFunction = Hotkeyfunction.StopServer;
        public Hotkeyfunction SelectedHotkeyFunction { get { return selectedHotkeyFunction; } set { selectedHotkeyFunction = value; OnHotkeyFunctionChanged(value); } }
        public string FhHotkeyHeaderText { get; private set; }
        public string HotkeyFunctionButtonText { get; private set; } = "Select hotkey";
        public bool FhAltChecked { get; private set; }
        public bool FhCtrlChecked { get; private set; }
        public bool FhShiftChecked { get; private set; }

        public ReactiveCommand HotkeyEnterCommand { get; }

        private void OnHotkeyFunctionChanged(Hotkeyfunction func)
        {
            FhHotkeyHeaderText = "Hotkey for function " + func;
            this.RaisePropertyChanged(nameof(FhHotkeyHeaderText));
            Hotkey hk = serverInstance.GetHotkey(func);
            HotkeyFunctionButtonText = hk.Key.ToString();
            this.RaisePropertyChanged(nameof(HotkeyFunctionButtonText));

            FhAltChecked = hk.Modifiers.HasFlag(HotkeyModifiers.Alt);
            FhCtrlChecked = hk.Modifiers.HasFlag(HotkeyModifiers.Ctrl);
            FhShiftChecked = hk.Modifiers.HasFlag(HotkeyModifiers.Shift);
            this.RaisePropertyChanged(nameof(FhAltChecked));
            this.RaisePropertyChanged(nameof(FhCtrlChecked));
            this.RaisePropertyChanged(nameof(FhShiftChecked));
        }

        private void ExecHotkeyEnter()
        {
            functionHotkeyEntering = true;
        }

        private bool functionHotkeyEntering = false;
        private void HandleKeyDownFunction(KeyEventArgs args)
        {
            if (!functionHotkeyEntering)
                return;

            //ignore modifier keys
            if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl
                || args.Key == Key.RightAlt || args.Key == Key.LeftAlt
                || args.Key == Key.LeftShift || args.Key == Key.RightShift)
                return;

            functionHotkeyEntering = false;
            SetFunctionHotkey(args.Key);
        }

        private void SetFunctionHotkey(Key key)
        {
            try
            {
                HotkeyModifiers mods = 0;
                if (FhAltChecked)
                    mods |= HotkeyModifiers.Alt;
                if (FhShiftChecked)
                    mods |= HotkeyModifiers.Shift;
                if (FhCtrlChecked)
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

                serverInstance.SetHotkey(SelectedHotkeyFunction, k);
                HotkeyFunctionButtonText = serverInstance.GetHotkey(SelectedHotkeyFunction).Key.ToString();
                this.RaisePropertyChanged(nameof(HotkeyFunctionButtonText));
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to set function hotkey: " + ex.Message);
            }
        }



        #endregion

        #region server settings

        private string mouseBufferRateText = "0";
        public string MouseBufferRateText { get { return mouseBufferRateText; } set { MouseBufferRateTextChanged(value); } }

        private void MouseBufferRateTextChanged(string text)
        {
            if (!int.TryParse(text, out int rate))
                return;

            mouseBufferRateText = text;
            this.RaisePropertyChanged(nameof(MouseBufferRateText));
            
            if(rate == 0)
            {
                //serverInstance.SetMouseInputMode(InputshareLib.Input.MouseInputMode.Realtime);
            }
            else
            {
                //serverInstance.SetMouseInputMode(InputshareLib.Input.MouseInputMode.Buffered, rate);
            }
        }
        #endregion
    }
}
