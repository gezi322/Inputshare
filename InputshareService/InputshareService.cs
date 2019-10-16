using InputshareLib;
using InputshareLib.Client;
using InputshareLibWindows;
using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.IPC.NamedIpc;
using InputshareLibWindows.Windows;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using static InputshareLib.Displays.DisplayManagerBase;

namespace InputshareService
{
    public sealed class InputshareService : ServiceBase
    {
        [DllImport("sas.dll")]
        public static extern void SendSAS(bool asUser);

        private ISClient clientInstance;
        private NamedIpcHost appHost;

        private AnonIpcHost iHostMain;
        private AnonIpcHost iHostDragDrop;

        private Process spMainProcess;
        private Process spDragDropProcess;

        static void Main(string[] args)
        {
            ServiceBase.Run(new InputshareService());
        }

        public InputshareService()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            ISLogger.SetLogFileName("InputshareService.log");
            ISLogger.EnableConsole = false;
            ISLogger.EnableLogFile = true;
            ISLogger.PrefixTime = true;
            CanHandleSessionChangeEvent = true;
        }

        protected override void OnStart(string[] args)
        {
            ISLogger.Write("Inputshare service starting...");
            ISLogger.Write("Console session state: " + Session.ConsoleSessionState);
            SetPriority();
            StartNamedIpcHost();
            

            iHostMain = new AnonIpcHost("SPMain");
            LaunchSPMain();

            iHostDragDrop = new AnonIpcHost("SPDragDrop");
            LaunchSPDragDrop();

            Task.Run(() =>
            {
                //TODO
                while (!iHostDragDrop.Connected || !iHostMain.Connected)
                {
                    Thread.Sleep(350);
                }
                Thread.Sleep(500);

                ISLogger.Write("IPC connected");

                DisplayConfig config = iHostMain.GetDisplayConfig();
                ISLogger.Write(config.VirtualBounds);

                clientInstance = new ISClient(WindowsDependencies.GetServiceDependencies(iHostMain, iHostDragDrop));
                //clientInstance.Connect("192.168.0.12", 4441, Environment.MachineName, Guid.NewGuid());
                clientInstance.ConnectionError += ClientInstance_ConnectionError;
                clientInstance.ConnectionFailed += ClientInstance_ConnectionFailed;
                clientInstance.Connected += ClientInstance_Connected;
                clientInstance.SasRequested += (object a, EventArgs b) => SendSAS(false);
                clientInstance.Disconnected += ClientInstance_Disconnected;
            });

            
            base.OnStart(args);
        }

        private void ClientInstance_Disconnected(object sender, EventArgs e)
        {
            ISLogger.Write("Disconnected.");
            SendAppStateUpdate();
        }

        private void ClientInstance_Connected(object sender, System.Net.IPEndPoint e)
        {
            SendAppStateUpdate();
            ISLogger.Write("Connected state = " + clientInstance.IsConnected);

        }

        private void StartNamedIpcHost()
        {
            appHost = new NamedIpcHost();
            appHost.ConnectionStateRequested += AppHost_ConnectionStateRequested;
            appHost.CommandConnect += AppHost_CommandConnect;
            appHost.Disconnect += AppHost_Disconnect;
            appHost.SetAutoReconnect += AppHost_SetAutoReconnect;
        }

        private void AppHost_SetAutoReconnect(object sender, bool e)
        {
            ISLogger.Write("IPC->Set auto reconnect " + e);

            clientInstance.AutoReconnect = e;
            SendAppStateUpdate();
        }

        private void AppHost_Disconnect(object sender, EventArgs e)
        {
            ISLogger.Write("IPC->Disconnect");

            if (clientInstance.IsConnected)
                clientInstance.Disconnect();
        }

        private void AppHost_CommandConnect(object sender, Tuple<System.Net.IPEndPoint, string> e)
        {
            ISLogger.Write("IPC->Connect to " + e.Item1.Address + ":" + e.Item1.Port + " as " + e.Item2);
            if (clientInstance.IsConnected)
                return;

            clientInstance.Connect(e.Item1.Address.ToString(), e.Item1.Port, e.Item2);
        }

        private void AppHost_ConnectionStateRequested(object sender, NamedIpcHost.StateRequestArgs e)
        {
            ISLogger.Write("IPC client requested state");
            e.Connected = clientInstance.IsConnected;
            e.clientId = clientInstance.ClientId;
            e.ClientName = clientInstance.ClientName;

            if (e.Connected)
                e.ConnectedAddress = clientInstance.ServerAddress;
        }

        private void ClientInstance_ConnectionFailed(object sender, string e)
        {
            ISLogger.Write("Failed to connect... " + e);
            ISLogger.Write("Connected state = " + clientInstance.IsConnected);
            SendAppStateUpdate();
        }

        private void ClientInstance_ConnectionError(object sender, string e)
        {
            ISLogger.Write("Connection error... " + e);
            ISLogger.Write("Connected state = " + clientInstance.IsConnected);
            SendAppStateUpdate();

        }

        private void SpMainProcess_Exited(object sender, EventArgs e)
        {
            spMainProcess?.Dispose();
            LaunchSPMain();
        }
        private void SpDragDropProcess_Exited(object sender, EventArgs e)
        {
            spDragDropProcess?.Dispose();

            if (Session.ConsoleSessionLoggedIn)
                LaunchSPDragDrop();
        }

        private void SendAppStateUpdate()
        {
            if (appHost.ClientConnected)
            {
                bool con = clientInstance.IsConnected;
                ISLogger.Write("Sending state. connected = " + con);
                if(con)
                    appHost.SendState(con, clientInstance.ServerAddress, clientInstance.ClientName, clientInstance.ClientId, clientInstance.AutoReconnect);
                else
                    appHost.SendState(con, new System.Net.IPEndPoint(IPAddress.Any, 0), clientInstance.ClientName, clientInstance.ClientId, clientInstance.AutoReconnect);

            }
        }

        private void LaunchSPMain()
        {
            try
            {
                ISLogger.Write("Launching SP default process");
                iHostMain.ReCreatePipeHost();
                IntPtr systemToken = Token.GetSystemToken(Session.ConsoleSessionId);
                spMainProcess = ProcessLauncher.LaunchSP(ProcessLauncher.SPMode.Default, WindowsDesktop.Default, false, iHostMain, systemToken);
                Token.CloseToken(systemToken);
                spMainProcess.EnableRaisingEvents = true;
                spMainProcess.Exited += SpMainProcess_Exited;
            }catch(Exception ex)
            {
                ISLogger.Write("Failed to launch inputshareSP main process: " + ex.Message);
            }
            
        }

        private void LaunchSPDragDrop()
        {
            try
            {
                ISLogger.Write("Launching SP dragdrop process");
                iHostDragDrop.ReCreatePipeHost();
                IntPtr userToken = Token.GetUserToken();
                spDragDropProcess = ProcessLauncher.LaunchSP(ProcessLauncher.SPMode.DragDrop, WindowsDesktop.Default, false, iHostDragDrop, userToken);
                Token.CloseToken(userToken);
                spDragDropProcess.EnableRaisingEvents = true;
                spDragDropProcess.Exited += SpDragDropProcess_Exited;
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to launch inputshareSP dragdrop process: " + ex.Message);
            }

        }

        

        protected override void OnStop()
        {
            ISLogger.Write("Inputshare service stopping...");

            ISLogger.Write("Killing child processes...");
            spMainProcess?.Kill();
            spDragDropProcess?.Kill();

            base.OnStop();
        }
        private void SetPriority()
        {
            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to set process priority: " + ex.Message);
            }
        }
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            ISLogger.Write("Session changed!");
            ISLogger.Write("Session change reason: " + changeDescription.Reason);
            ISLogger.Write("Session ID: " + changeDescription.SessionId);
            ISLogger.Write("Session state: " + Session.ConsoleSessionState);

            if(changeDescription.Reason == SessionChangeReason.SessionLogon)
            {
                if(spDragDropProcess != null)
                    LaunchSPDragDrop();
            }

            base.OnSessionChange(changeDescription);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            ISLogger.Write("---------------------------------");
            ISLogger.Write("Unhandled exception!");
            ISLogger.Write(ex.Message);
            ISLogger.Write(ex.StackTrace);
            ISLogger.Write("---------------------------------");

            Stop(); 
        }
    }
}
