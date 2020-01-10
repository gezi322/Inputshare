using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.ViewModels
{
    internal class WinServiceBaseViewModel : ViewModelBase
    {
       


#if WindowsBuild
        public override event EventHandler Leave;

        public ViewModelBase CurrentView { get; private set; }
        private WinServiceConnectedViewModel connectedVM;
        private WinServiceDisconnectedViewModel disconnectedVM;
        

        private ISWinServiceModel model;

        public WinServiceBaseViewModel()
        {
            BottomButtonText = "Back";
        }

       public override void HandleBottomButtonPressed()
        {
            if (model != null && model.ServiceConnected)
            {
                model.DisconnectService();
            }
        }

        public async override void OnShow()
        {
            model = new ISWinServiceModel();
            model.ServiceConnectionLost += Model_ServiceConnectionLost;
            model.Connected += Model_Connected;
            model.Disconnected += Model_Disconnected;

            if (!model.ConnectService())
            {
                Leave?.Invoke(this, null);
                return;
            }

            connectedVM = new WinServiceConnectedViewModel(model);
            disconnectedVM = new WinServiceDisconnectedViewModel(model);

            bool connected = await model.GetConnectedStateAsync();

            if (connected)
                SetSubViewModel(connectedVM);
            else
                SetSubViewModel(disconnectedVM);
        }

        private void Model_Disconnected(object sender, EventArgs e)
        {
            SetSubViewModel(disconnectedVM);
        }

        private void Model_Connected(object sender, EventArgs e)
        {
            SetSubViewModel(connectedVM);
        }

        private void SetSubViewModel(ViewModelBase vm)
        {
            CurrentView = vm;
            this.RaisePropertyChanged(nameof(CurrentView));
        }

        private void Model_ServiceConnectionLost(object sender, string e)
        {
            Leave?.Invoke(this, null);
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
