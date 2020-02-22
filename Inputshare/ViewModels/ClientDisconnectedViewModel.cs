using Inputshare.Common.Client;
using Inputshare.Common.Net.Broadcast;
using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.ViewModels
{
    public class ClientDisconnectedViewModel : ViewModelBase
    {
        public override string BottomButtonText { get; protected set; } = "Back";

        public ReactiveCommand<Unit, Unit> CommandConnect { get; private set; }
        public ReactiveCommand<Unit, Unit> CommandConnectSelected { get; private set; }
        public ObservableCollection<IPEndPoint> ServerAddressList { get; } = new ObservableCollection<IPEndPoint>();


        private IPEndPoint _selectedServerAddress;
        public IPEndPoint ServerSelectedAddress { get => _selectedServerAddress; set => OnSelectedServerAddressChange(value); }
        private bool _validServerAddress => ServerSelectedAddress != null;

        private string _clientNameEntryText;
        public string ClientNameEntryText { get => _clientNameEntryText; set => OnClientNameEntryChanged(value); }

        private bool _validAddressEntry => IPEndPoint.TryParse(_addressEntryText, out _);
        private string _addressEntryText = "192.168.0.1:1234";
        public string AddressEntryText { get => _addressEntryText; set => OnAddressEntryChanged(value); }

        private ClientModel _model;

        public ClientDisconnectedViewModel(ClientModel model)
        {
            _model = model;
            _model.BroadcastedAddressReceived += OnBroadcastAddressReceived;
            ClientNameEntryText = model.ClientName;
            CommandConnect = ReactiveCommand.CreateFromTask(ExecConnect, this.WhenAnyValue(i => i._validAddressEntry));
            CommandConnectSelected = ReactiveCommand.CreateFromTask(ExecConnectSelected, this.WhenAnyValue(i => i._validServerAddress));
        }

        private async Task ExecConnect()
        {
            await _model.ConnectAsync(IPEndPoint.Parse(AddressEntryText));
        }

        private void OnAddressEntryChanged(string value)
        {
            _addressEntryText = value;
            this.RaisePropertyChanged(nameof(AddressEntryText));
            this.RaisePropertyChanged(nameof(_validAddressEntry));
        }

        private void OnClientNameEntryChanged(string value)
        {
            if(_model != null)
            {
                _model.ClientName = value;
                _clientNameEntryText = value;
                this.RaisePropertyChanged(nameof(ClientNameEntryText));
            }
        }

        private void OnSelectedServerAddressChange(IPEndPoint selected)
        {
            _selectedServerAddress = selected;
            this.RaisePropertyChanged(nameof(ServerSelectedAddress));
            this.RaisePropertyChanged(nameof(_validServerAddress));
        }

        private void OnBroadcastAddressReceived(object sender, BroadcastReceivedArgs args)
        {
            if (!ServerAddressList.Contains(args.Server))
            {
                ServerAddressList.Add(args.Server);
            }
        }

        private async Task ExecConnectSelected()
        {

            if (ServerSelectedAddress != null)
                await _model.ConnectAsync(ServerSelectedAddress);
        }

        public override void OnBottomButtonPress()
        {

        }

        public override void HandleWindowClosing()
        {

        }
    }
}
