using System;
using System.Collections.Generic;
using System.Text;
using InputshareB.Views;
using InputshareLib;
using InputshareLib.Client;
using InputshareLib.Server;
using ReactiveUI;

namespace InputshareB.ViewModels
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

        public MainWindowViewModel()
        {
            ISLogger.EnableConsole = true;
            ISLogger.EnableLogFile = false;

            ISServer server = CreateServerInstance();

            serverView = new ServerStoppedViewModel(server);
            serverStartedView = new ServerStartedViewModel(server);
            server.Started += Server_Started;
            server.Stopped += Server_Stopped;

            selectView = new ModeSelectViewModel();
            selectView.Switchserver += SelectView_Switchserver;
            selectView.SwitchClient += SelectView_SwitchClient;
            selectView.SwitchService += SelectView_SwitchService;

            ISClient client = CreateClientInstance();
            clientView = new ClientViewModel(client);
            clientConnectedView = new ClientConnectedViewModel(client);

            client.Connected += Client_Connected;
            client.ConnectionError += Client_ConnectionError;
            client.Disconnected += Client_Disconnected;

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

        private ISServer CreateServerInstance()
        {
            ISServer server;
#if WindowsBuild
            server = new ISServer(InputshareLibWindows.WindowsDependencies.GetServerDependencies(), new InputshareLib.StartOptions(new List<string>()));
#elif LinuxBuild
            server = new ISServer(ISServerDependencies.GetLinuxDependencies(), new InputshareLib.StartOptions(new List<string>()));
#else
            throw new NotImplementedException("OS not supported");
#endif
            return server;
        }

        private ISClient CreateClientInstance()
        {


#if WindowsBuild
            return new ISClient(InputshareLibWindows.WindowsDependencies.GetClientDependencies(), new InputshareLib.StartOptions(new List<string>()));
#elif LinuxBuild
            return  new ISClient(ISClientDependencies.GetLinuxDependencies(), new InputshareLib.StartOptions(new List<string>()));
#else
            throw new Exception("OS not supported");
#endif
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
    }
}
