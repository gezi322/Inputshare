using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Inputshare.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public event EventHandler Leave;

        public event EventHandler SwitchClient;
        public event EventHandler SwitchServer;
        public event EventHandler SwitchWinService;

#if WindowsBuild
        public bool ShowWinService { get; } = true;
#else
        public bool ShowWinService { get; } = false;
#endif

        public ReactiveCommand<Unit, Unit> CommandClient { get; }
        public ReactiveCommand<Unit, Unit> CommandServer { get; }
        public ReactiveCommand<Unit, Unit> CommandWinService { get; }


        public HomeViewModel()
        {
            CommandClient = ReactiveCommand.Create(ExecClient);
            CommandServer = ReactiveCommand.Create(ExecServer);
            CommandWinService = ReactiveCommand.Create(ExecWinService);

            BottomButtonText = "Exit";
        }

        private void ExecClient()
        {
            SwitchClient?.Invoke(this, null);
        }

        private void ExecServer()
        {
            SwitchServer?.Invoke(this, null);
        }

        private void ExecWinService()
        {
            SwitchWinService?.Invoke(this, null);
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
