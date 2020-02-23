using Inputshare.Common.Server;
using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.ViewModels
{
    public class ServerStoppedViewModel : ViewModelBase
    {
        public override string BottomButtonText { get; protected set; } = "Back";

        public ReactiveCommand<Unit, Unit> CommandStart { get; private set; }

        private bool _validPortEntry => int.TryParse(_portEntryText, out _);
        private string _portEntryText = "4441";
        public string PortEntryText { get => _portEntryText; set => OnPortEntryTextChanged(value); }

        private ServerModel _model;

        public ServerStoppedViewModel(ServerModel model)
        {
            _model = model;
            CommandStart = ReactiveCommand.CreateFromTask(ExecStart, this.WhenAnyValue(i => i._validPortEntry));
        }

        private void OnPortEntryTextChanged(string value)
        {
            _portEntryText = value;
            this.RaisePropertyChanged(nameof(_validPortEntry));
            this.RaisePropertyChanged(nameof(PortEntryText));
        }

        private async Task ExecStart()
        {
            await _model.StartAsync(int.Parse(_portEntryText));
        }

        public override Task HandleWindowClosingAsync()
        {
            return Task.CompletedTask;
        }

        public override Task HandleBottomButtonPressAsync()
        {
            return Task.CompletedTask;
        }
    }
}
