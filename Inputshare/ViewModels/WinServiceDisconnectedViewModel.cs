using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive;
using System.Text;

namespace Inputshare.ViewModels
{
    internal class WinServiceDisconnectedViewModel : ViewModelBase
    {
        public override void HandleBottomButtonPressed()
        {

        }

        public override void HandleExit()
        {

        }

#if WindowsBuild
        private ISWinServiceModel model;

        private string _addressEntryText = "192.168.0.12:4441";
        public string AddressEntryText
        {
            get
            {
                return _addressEntryText;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _addressEntryTextValid, IPEndPoint.TryParse(value, out _));
                _addressEntryText = value;
            }
        }

        private bool _addressEntryTextValid;
        public bool AddressEntryTextValid { get => _addressEntryTextValid; }

        private string _clientNameEntryText = Environment.MachineName;
        public string ClientNameEntryText
        {
            get
            {
                return _clientNameEntryText;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _ClientNameEntryTextValid, value.Length != 0 && value.Length < 32);
                _clientNameEntryText = value;
            }
        }

        private bool _ClientNameEntryTextValid;
        public bool ClientNameEntryTextValid { get => _ClientNameEntryTextValid; }

        public ReactiveCommand<Unit, Unit> CommandConnect { get; }

        public WinServiceDisconnectedViewModel(ISWinServiceModel model)
        {
            this.model = model;
            ClientNameEntryText = Environment.MachineName;
            AddressEntryText = "192.168.0.12:4441";
            CommandConnect = ReactiveCommand.Create(ExecConnect);
        }
        
        private void ExecConnect()
        {
            if(ClientNameEntryTextValid && AddressEntryTextValid)
            {
                model.SetName(_clientNameEntryText);
                model.ClientConnect(IPEndPoint.Parse(_addressEntryText));
            }
        }
#endif
    }
}
