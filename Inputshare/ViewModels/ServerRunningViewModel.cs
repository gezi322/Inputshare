using Avalonia.Threading;
using Inputshare.Models;
using InputshareLib.Server;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Key = Avalonia.Input.Key;
using KeyEventArgs = Avalonia.Input.KeyEventArgs;

namespace Inputshare.ViewModels
{
    internal class ServerRunningViewModel : ViewModelBase
    {

        private ISServerModel model;

        private ISClientInfoModel _selectedClient = ISClientInfoModel.None;

        public ISClientInfoModel SelectedClient { get
            {
                return _selectedClient;
            } set
            {
                HandleSelectedClientChanged(value);
            }
        }

        public bool ClientSettingsVisible { get; private set; }
        public ISClientInfoModel CurrentInputClient { get; private set; } = ISClientInfoModel.None;
        public ObservableCollection<ISClientInfoModel> ClientList { get; } = new ObservableCollection<ISClientInfoModel>();
        public ISHotkeyModel ClientHotkey { get; private set; }
        public ReactiveCommand<Unit, Unit> CommandClientHotkey { get; }

        private bool hotkeyEntering = false;

        public ServerRunningViewModel(ISServerModel model)
        {
            this.model = model;
            model.ClientConnected += Model_ClientConnected;
            model.ClientDisconnected += Model_ClientDisconnected;
            model.InputClientSwitched += Model_InputClientSwitched;
            model.ServerStarted += Model_ServerStarted;
            CommandClientHotkey = ReactiveCommand.Create(ExecClientHotkey);
        }

        public override void HandleBottomButtonPressed()
        {
            ExecStopServer();
        }

        private void Model_ServerStarted(object sender, EventArgs e)
        {
            BottomButtonText = "Stop server";
            this.RaisePropertyChanged(nameof(CurrentInputClient));
            ClientList.Clear();

            ISClientInfoModel local = model.GetLocalHost();
            ClientList.Add(ISClientInfoModel.None);
            ClientList.Add(local);
            CurrentInputClient = local;
        }

        private void Model_InputClientSwitched(ISClientInfoModel obj)
        {
            Dispatcher.UIThread.InvokeAsync(() => {
                CurrentInputClient = obj;
                this.RaisePropertyChanged(nameof(CurrentInputClient));
            });
        }

        private void Model_ClientDisconnected(ISClientInfoModel obj)
        {
            Dispatcher.UIThread.InvokeAsync(() => {
                if (SelectedClient == obj)
                {
                    SelectedClient = model.GetLocalHost();
                    this.RaisePropertyChanged(nameof(SelectedClient));
                }

                ClientList.Remove(obj);
            });
        }

        private void Model_ClientConnected(ISClientInfoModel obj)
        {
            Dispatcher.UIThread.InvokeAsync(() => {
                ClientList.Add(obj);
            });
        }

        private void HandleSelectedClientChanged(ISClientInfoModel client)
        {
            if (client == null)
            {
                SelectedClient = model.GetLocalHost();
                this.RaisePropertyChanged(nameof(SelectedClient));
                return;
            }

            ClientSettingsVisible = client != ISClientInfoModel.None;
            this.RaisePropertyChanged(nameof(ClientSettingsVisible));
            ClientHotkey = new ISHotkeyModel(client.ClientHotkey);
            this.RaisePropertyChanged(nameof(ClientHotkey));
            _selectedClient = client;
            this.RaisePropertyChanged(nameof(SelectedClient));

            hotkeyEntering = false;
        }

        private void ExecStopServer()
        {
            model.StopServer();
        }

        private void ExecClientHotkey()
        {
            hotkeyEntering = true;
        }

        public override void HandleKeyPress(KeyEventArgs args)
        {
            if (hotkeyEntering)
            {
                if (args.Key != 0 && args.Key != Key.LeftAlt && args.Key != Key.RightAlt && args.Key != Key.RightCtrl
                    && args.Key != Key.LeftCtrl && args.Key != Key.RightShift && args.Key != Key.LeftShift &&
                    args.Key != Key.LWin && args.Key != Key.RWin)
                {
                    hotkeyEntering = false;
                    ClientHotkey.Key = args.Key;

                    model.SetClientHotkey(SelectedClient, ClientHotkey);
                    ClientHotkey = new ISHotkeyModel(SelectedClient.ClientHotkey);
                    this.RaisePropertyChanged(nameof(ClientHotkey));
                }
            }
           
        }

        public override void HandleExit()
        {
            model.StopServer();
        }
    }
}
