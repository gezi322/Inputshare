using Inputshare.Models;
using InputshareLib;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Inputshare.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public event EventHandler CloseWindow;

        public ViewModelBase CurrentView { get; private set; }
        public ReactiveCommand<Unit, Unit> CommandBottomButton { get; }

        private ISServerModel serverModel;
        private ISClientModel clientModel;

        private ServerRunningViewModel serverRunningVM;
        private ServerStoppedViewModel serverStoppedVM;
        private ClientDisconnectedViewModel clientDisconnectedVM;
        private ClientConnectedViewModel clientConnectedVM;
#if WindowsBuild
        private WinServiceViewModel winServiceVM;
#endif
        private HomeViewModel homeVM;

        public MainWindowViewModel()
        {
            ISLogger.EnableConsole = true;
            homeVM = new HomeViewModel();
            homeVM.SwitchWinService += HomeVM_SwitchWinService;
            homeVM.SwitchClient += HomeVM_SwitchClient;
            homeVM.SwitchServer += HomeVM_SwitchServer;
            homeVM.Leave += HomeVM_Leave;

            serverModel = new ISServerModel();
            serverRunningVM = new ServerRunningViewModel(serverModel);
            serverStoppedVM = new ServerStoppedViewModel(serverModel);
            serverStoppedVM.Leave += ServerStoppedVM_Leave;
            serverModel.ServerStarted += ServerModel_ServerStarted;
            serverModel.ServerStopped += ServerModel_ServerStopped;
            

            clientModel = new ISClientModel();
            clientDisconnectedVM = new ClientDisconnectedViewModel(clientModel);
            clientDisconnectedVM.Leave += ClientDisconnectedVM_Leave;
            clientConnectedVM = new ClientConnectedViewModel(clientModel);
            clientModel.Connected += ClientModel_Connected;
            clientModel.Disconnected += ClientModel_Disconnected;

#if WindowsBuild
            winServiceVM = new WinServiceViewModel();
            winServiceVM.Leave += WinServiceVM_Leave;
#endif

            SetViewModel(homeVM);
            CommandBottomButton = ReactiveCommand.Create(OnBottomButtonPress);
        }

        private void HomeVM_Leave(object sender, EventArgs e)
        {
            CloseWindow?.Invoke(this, null);
        }

        private void WinServiceVM_Leave(object sender, EventArgs e)
        {
            SetViewModel(homeVM);
        }

        private void HomeVM_SwitchWinService(object sender, EventArgs e)
        {
#if WindowsBuild
            SetViewModel(winServiceVM);
#endif
        }

        private void ClientDisconnectedVM_Leave(object sender, EventArgs e)
        {
            SetViewModel(homeVM);
        }

        private void ServerStoppedVM_Leave(object sender, EventArgs e)
        {
            SetViewModel(homeVM);
        }

        private void ClientModel_Disconnected(object sender, EventArgs e)
        {
            SetViewModel(clientDisconnectedVM);
        }

        private void ClientModel_Connected(object sender, EventArgs e)
        {
            SetViewModel(clientConnectedVM);
        }

        private void HomeVM_SwitchServer(object sender, EventArgs e)
        {
            SetViewModel(serverStoppedVM);
        }

        private void HomeVM_SwitchClient(object sender, EventArgs e)
        {
            SetViewModel(clientDisconnectedVM);
        }

        private void ServerModel_ServerStopped(object sender, EventArgs e)
        {
            SetViewModel(serverStoppedVM);
        }

        private void ServerModel_ServerStarted(object sender, EventArgs e)
        {
            SetViewModel(serverRunningVM);
        }

        private void SetViewModel(ViewModelBase model)
        {
            CurrentView = model;
            this.RaisePropertyChanged(nameof(CurrentView));
        }

        private void OnBottomButtonPress()
        {
            CurrentView.HandleBottomButtonPressed();
        }

        public override void HandleBottomButtonPressed()
        {
            Console.WriteLine("----");
            CloseWindow?.Invoke(this, null);
        }

        public override void HandleExit()
        {

        }
    }
}
