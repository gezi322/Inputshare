using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Inputshare.ViewModels
{
    internal class ServerStoppedViewModel : ViewModelBase
    {
        public event EventHandler Leave;

        private ISServerModel model;

        private string _portEntryText = "4441";
        public string PortEntryText
        {
            get
            {
                return _portEntryText;
            }
            set
            {
                OnPortTextChanged(value);
            }
        }

        public ISServerStartOptionsModel StartOptions { get; } = new ISServerStartOptionsModel();
        public bool ValidPortEntry { get; private set; }
        public ReactiveCommand<Unit, Unit> CommandStartServer { get; }

        public ServerStoppedViewModel(ISServerModel model)
        {
            CommandStartServer = ReactiveCommand.Create(ExecStartServer);
            this.model = model;
            BottomButtonText = "Back";
            OnPortTextChanged(_portEntryText);
        }

        private void ExecStartServer()
        {
            if (ValidPortEntry)
            {
                int port = int.Parse(PortEntryText);
                Console.WriteLine("port = " + port);
                model.StartServer(port, StartOptions);
            }
        }

        private void OnPortTextChanged(string text)
        {
            ValidPortEntry = int.TryParse(text, out _);
            this.RaisePropertyChanged(nameof(ValidPortEntry));
            _portEntryText = text;
        }

        public override void HandleBottomButtonPressed()
        {
            Leave?.Invoke(this, null);
        }

        public override void HandleExit()
        {

        }
    }
}
