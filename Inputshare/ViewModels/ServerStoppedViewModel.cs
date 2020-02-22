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

        private ServerModel _model;

        public ServerStoppedViewModel(ServerModel model)
        {
            _model = model;
            CommandStart = ReactiveCommand.CreateFromTask(ExecStart);
        }

        private async Task ExecStart()
        {
            await _model.StartAsync(4444);
        }

        public override void HandleWindowClosing()
        {

        }

        public override void OnBottomButtonPress()
        {

        }
    }
}
