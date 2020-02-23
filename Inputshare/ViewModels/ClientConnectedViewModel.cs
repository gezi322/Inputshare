using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.ViewModels
{
    public class ClientConnectedViewModel : ViewModelBase
    {
        public override string BottomButtonText { get; protected set; } = "Disconnect";
        public IPEndPoint ServerAddress => _model.ServerAddress;

        private ClientModel _model;

        public ClientConnectedViewModel(ClientModel model)
        {
            _model = model;
            _model.Connected += OnConnected;
        }

        private void OnConnected(object sender, EventArgs e)
        {
            this.RaisePropertyChanged(nameof(ServerAddress));
        }

        public override Task HandleBottomButtonPressAsync()
        {
            _model.Disconnect();

            return Task.CompletedTask;
        }

        public override Task HandleWindowClosingAsync()
        {
            return Task.CompletedTask;
        }
    }
}
