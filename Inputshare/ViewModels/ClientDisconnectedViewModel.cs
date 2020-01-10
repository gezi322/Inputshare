using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive;
using System.Text;

namespace Inputshare.ViewModels
{
    internal class ClientDisconnectedViewModel : ViewModelBase
    {
        public event EventHandler Leave;

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

        public ISClientStartOptionsModel StartOptions { get; } = new ISClientStartOptionsModel();

        private ISClientModel model;
        private ReactiveCommand<Unit, Unit> CommandConnect { get; }
        public ClientDisconnectedViewModel(ISClientModel model)
        {
            this.model = model;
            CommandConnect = ReactiveCommand.Create(ExecConnect);
            BottomButtonText = "Back";

            _addressEntryTextValid= IPEndPoint.TryParse(AddressEntryText, out _);
            _ClientNameEntryTextValid = _clientNameEntryText.Length > 0 && _clientNameEntryText.Length < 32;
        }

        private void ExecConnect()
        {
            if (AddressEntryTextValid && ClientNameEntryTextValid)
                model.Connect(StartOptions, IPEndPoint.Parse(AddressEntryText), ClientNameEntryText);
        }

        public override void HandleBottomButtonPressed()
        {
            model.StopIfRunning();
            Leave?.Invoke(this, null);
        }

        public override void HandleExit()
        {
            model.StopIfRunning();
        }
    }
}
