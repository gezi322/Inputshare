using Inputshare.Models;
using InputshareLib;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive;
using System.Text;

namespace Inputshare.ViewModels
{
    internal class WinServiceConnectedViewModel : ViewModelBase
    {

#if WindowsBuild
        private ISWinServiceModel model;

        public ReactiveCommand<Unit, Unit> CommandDisconnect { get; }
        public string InfoText { get; private set; } = "unset";

        public WinServiceConnectedViewModel(ISWinServiceModel model)
        {
            this.model = model;
            CommandDisconnect = ReactiveCommand.Create(ExecDisconnect);
        }

        private void ExecDisconnect()
        {
            model.ClientDisconnect();
        }

        public override void HandleBottomButtonPressed()
        {
        }

        public override void HandleExit()
        {
        }
#else
        public override void HandleBottomButtonPressed()
        {
        }

        public override void HandleExit()
        {
        }
#endif
    }
}
