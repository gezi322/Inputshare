using Avalonia.Threading;
using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.ViewModels
{
    public class ClientViewModel : ViewModelBase
    {
        private string _t;
        public override string BottomButtonText { get => SelectedView.BottomButtonText; protected set => _t = value; }
        public override event EventHandler Leave;

        public ViewModelBase SelectedView { get; protected set; }

        private ClientModel _model;
        private ClientDisconnectedViewModel _disconnectedVM;
        private ClientConnectedViewModel _connectedVM;

        public ClientViewModel()
        {
            _model = new ClientModel();
            _model.Connected += OnClientConnect;
            _model.Disconnected += OnClientDisconnect;
            _disconnectedVM = new ClientDisconnectedViewModel(_model);
            _connectedVM = new ClientConnectedViewModel(_model);
            SelectedView = _disconnectedVM;
            _model.MonitorBroadcasts = true;
        }

        private void OnClientDisconnect(object sender, string e)
        {
            SetViewModel(_disconnectedVM);
            _model.MonitorBroadcasts = true;
        }

        private void OnClientConnect(object sender, EventArgs e)
        {
            SetViewModel(_connectedVM);
            _model.MonitorBroadcasts = false;
        }

        private void SetViewModel(ViewModelBase vm)
        {
            SelectedView = vm;
            this.RaisePropertyChanged(nameof(SelectedView));
            this.RaisePropertyChanged(nameof(BottomButtonText));
        }

        public override async Task HandleBottomButtonPressAsync()
        {
            if(SelectedView is ClientDisconnectedViewModel)
            {
                if (_model.ClientRunning)
                    await _model.StopAsync();

                Leave?.Invoke(this, null);
            }
            else
            {
                await SelectedView.HandleBottomButtonPressAsync();
            }
            
        }

        public override async Task HandleWindowClosingAsync()
        {
            if (_model.ClientRunning)
                await _model.StopAsync();
        }
    }
}
