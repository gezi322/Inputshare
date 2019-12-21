#if WindowsBuild
using Inputshare.Models;
using InputshareLibWindows.IPC.NetIpc;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Inputshare.ViewModels
{
    internal class WinServiceViewModel : ViewModelBase
    {
        public event EventHandler Leave;
        public bool ShowServiceConnect { get; private set; } = true;
        private ISWinServiceModel model = new ISWinServiceModel();
        public string ServiceConnectText { get; private set; }
        public ReactiveCommand<Unit, Unit> CommandConnectService { get; }

        internal WinServiceViewModel()
        {
            BottomButtonText = "Back";
            ServiceConnectText = "Not connected to Inputshare service";
            CommandConnectService = ReactiveCommand.Create(ExecConnectService);
            model.ServiceConnectionLost += Model_ServiceConnectionLost;
        }

        private void Model_ServiceConnectionLost(object sender, string e)
        {
            ShowServiceConnect = false;
            this.RaisePropertyChanged(nameof(ShowServiceConnect));
            ServiceConnectText = "Lost connection to Inputshare service: " + e;
            this.RaisePropertyChanged(nameof(ServiceConnectText));
            BottomButtonText = "Back";
            this.RaisePropertyChanged(nameof(BottomButtonText));
        }

        private void ExecConnectService() 
        {
            if (!model.ServiceConnected)
            {
                if (model.ConnectService())
                {
                    ShowServiceConnect = false;
                    this.RaisePropertyChanged(nameof(ShowServiceConnect));
                    BottomButtonText = "Disconnect from service";
                }
                else
                {
                    ServiceConnectText = "Could not connect to Inputshare service...";
                    this.RaisePropertyChanged(nameof(ServiceConnectText));
                }
            }
        }

        public override void HandleBottomButtonPressed()
        {
            if (model.ServiceConnected)
            {
                model.DisconnectService();
                ShowServiceConnect = true;
                this.RaisePropertyChanged(nameof(ShowServiceConnect));
                ServiceConnectText = "Disconnected from Inputshare service...";
                this.RaisePropertyChanged(nameof(ServiceConnectText));
            }
            else
            {
                Leave?.Invoke(this, null);
            }
        }

        public override void HandleExit()
        {
            if(model != null && model.ServiceConnected)
            {
                model.DisconnectService();
            }
        }
    }
}
#endif