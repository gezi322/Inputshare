using System;
using System.Collections.Generic;
using System.Text;
using Inputshare.Views;
using InputshareLib;
using InputshareLib.Client;
using InputshareLib.Server;
using ReactiveUI;

namespace Inputshare.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public bool ServiceOptionEnabled { get; private set; }

        private ServerStoppedViewModel serverView;
        private ServerStartedViewModel serverStartedView;
        private ClientViewModel clientView;
        private ClientConnectedViewModel clientConnectedView;
        private WindowsServiceViewModel serviceView;

        private ModeSelectViewModel selectView;

        public ViewModelBase CurrentModel { get; private set; }

        private ISServer serverInstance;
        private ISClient clientInstance;

        public MainWindowViewModel()
        {
            ISLogger.EnableConsole = true;
            ISLogger.EnableLogFile = false;

            serverInstance = new ISServer();

            serverView = new ServerStoppedViewModel(serverInstance);
            serverStartedView = new ServerStartedViewModel(serverInstance);
            serverInstance.Started += Server_Started;
            serverInstance.Stopped += Server_Stopped;

            selectView = new ModeSelectViewModel();
            selectView.Switchserver += SelectView_Switchserver;
            selectView.SwitchClient += SelectView_SwitchClient;
            selectView.SwitchService += SelectView_SwitchService;

            clientInstance = new ISClient();
            clientView = new ClientViewModel(clientInstance);
            clientConnectedView = new ClientConnectedViewModel(clientInstance);

            clientInstance.Connected += Client_Connected;
            clientInstance.ConnectionError += Client_ConnectionError;
            clientInstance.Disconnected += Client_Disconnected;

            serviceView = new WindowsServiceViewModel();

            CurrentModel = selectView;
        }

        private void SelectView_SwitchService(object sender, EventArgs e)
        {
            ChangeViewModel(serviceView);
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            ChangeViewModel(clientView);
        }

        private void Client_ConnectionError(object sender, string e)
        {
            ChangeViewModel(clientView);
        }

        private void Client_Connected(object sender, System.Net.IPEndPoint e)
        {
            ChangeViewModel(clientConnectedView);
        }

        private void Server_Stopped(object sender, EventArgs e)
        {
            ChangeViewModel(serverView);
        }

        private void Server_Started(object sender, EventArgs e)
        {
            ChangeViewModel(serverStartedView);
        }

        private void SelectView_SwitchClient(object sender, EventArgs e)
        {
            ChangeViewModel(clientView);
        }

        private void SelectView_Switchserver(object sender, EventArgs e)
        {
            ChangeViewModel(serverView);
        }

        private void ChangeViewModel(ViewModelBase view)
        {
            CurrentModel = view;
            this.RaisePropertyChanged(nameof(CurrentModel));
        }

        public void HandleExit()
        {
            if (serverInstance.Running)
                serverInstance.Stop();

            if (clientInstance.Running)
                clientInstance.Stop();
        }
    }
}
