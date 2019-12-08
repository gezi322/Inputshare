using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib;
using ReactiveUI;

namespace InputshareB.ViewModels
{
    public class ModeSelectViewModel : ViewModelBase
    {
        public event EventHandler Switchserver;
        public event EventHandler SwitchClient;
        public event EventHandler SwitchService;

        private MainWindowViewModel main { get; }

        public bool ServiceOptionEnabled { get; private set; } = false;
        public ReactiveCommand SwitchToServer { get; }
        public ReactiveCommand SwitchToClient { get; }
        public ReactiveCommand SwitchToService { get; }

        public ModeSelectViewModel()
        {
            SwitchToServer = ReactiveCommand.Create(ExecSwitchToServer);
            SwitchToClient = ReactiveCommand.Create(ExecSwitchToClient);
            SwitchToService = ReactiveCommand.Create(ExecSwitchToService);

#if WindowsBuild
            ServiceOptionEnabled = true;
#endif
        }

        private void ExecSwitchToService()
        {
            SwitchService?.Invoke(this, null);
        }

        private void ExecSwitchToServer()
        {
            Switchserver?.Invoke(this, null);
        }
        private void ExecSwitchToClient()
        {
            SwitchClient?.Invoke(this, null);
        }
    }
}
