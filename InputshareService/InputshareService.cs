using InputshareLib;
using InputshareLib.Client;
using InputshareLibWindows;
using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.IPC.NetIpc;
using InputshareLibWindows.Windows;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareService
{
    public sealed class InputshareService : ServiceBase
    {
        private ISClient clientInstance;

        private AnonIpcHost iHostMain;
        private AnonIpcHost iHostDragDrop;
        private NetIpcHost appHost;

        private IpcHandle spMainHandle = new IpcHandle();
        private IpcHandle spDragDropHandle = new IpcHandle();

        /// <summary>
        /// True if the service is stopping.
        /// </summary>
        private bool stopping = false;

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

            Task.Run(() => { SpDragDropTaskLoop(); });
            Task.Run(() => { SpMainTaskLoop(); });


            Task.Run(() => { LoadAndStart(); });

            base.OnStart(args);
        }

        private void SpDragDropTaskLoop()
        {
            iHostDragDrop = new AnonIpcHost("Dragdrop process");
            while (!stopping)
            {
                try
                {
                    Process proc = LaunchSPDragDrop();
                    proc.WaitForExit();
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                }

            }
        }

        private void SpMainTaskLoop()
        {
            iHostMain = new AnonIpcHost("Main process");

            while (!stopping)
            {
                try
                {
                    Process proc = LaunchSPMain();
                    proc.WaitForExit();
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private LoadedConfiguration LoadConfig()
        {
            try
            {
                string name = Config.Read(Config.ConfigProperty.LastClientName);
                Guid id = new Guid(Config.Read(Config.ConfigProperty.LastClientGuid));
                bool connected = Config.Read(Config.ConfigProperty.LastConnectionState) == "True";
                IPEndPoint.TryParse(Config.Read(Config.ConfigProperty.LastConnectedAddress), out IPEndPoint address);

                IPEndPoint lastAddr = new IPEndPoint(IPAddress.Any, 0);
                if (address != null)
                    lastAddr = address;

                bool autoReconnect = Config.Read(Config.ConfigProperty.AutoReconnectEnabled) == "True";
                return new LoadedConfiguration(connected, lastAddr, name, id, autoReconnect);

            }catch(Exception ex)
            {
                ISLogger.Write("Failed to load configuration: " + ex.Message);
                return new LoadedConfiguration(false, new IPEndPoint(IPAddress.Any, 0), Environment.MachineName, Guid.NewGuid(), false) ;
            }
            
        }

        private void ClientInstance_Disconnected(object sender, EventArgs e)
        {
            OnStateChange();
            ISLogger.Write("Disconnected.");
        }

        private void ClientInstance_Connected(object sender, System.Net.IPEndPoint e)
        {
            OnStateChange();
        }

        private void ClientInstance_ConnectionFailed(object sender, string error)
        {
            OnStateChange();
        }

        private void ClientInstance_ConnectionError(object sender, string error)
        {
            OnStateChange();
        }

        private void OnStateChange()
        {
            SaveConfig();
        }


        private void LoadAndStart()
        {
            try
            {
                LoadedConfiguration conf = LoadConfig();
                clientInstance = new ISClient(WindowsDependencies.GetServiceDependencies(spMainHandle, spDragDropHandle));
                

                clientInstance.ConnectionError += ClientInstance_ConnectionError;
                clientInstance.ConnectionFailed += ClientInstance_ConnectionFailed;
                clientInstance.Connected += ClientInstance_Connected;
                clientInstance.SasRequested += (object a, EventArgs b) => InputshareLibWindows.Native.Sas.SendSAS(false);
                clientInstance.Disconnected += ClientInstance_Disconnected;
                clientInstance.AutoReconnect = conf.AutoReconnect;

                try
                {
                    appHost = new NetIpcHost(clientInstance, "App connection");
                }catch(Exception ex)
                {
                    ISLogger.Write("Failed to create NetIPC host: " + ex.Message);
                }
                

                if (conf.ClientName != "")
                    clientInstance.SetClientName(conf.ClientName);

                if (conf.ClientGuid != Guid.Empty)
                    clientInstance.SetClientGuid(conf.ClientGuid);


                if (conf.Address.ToString() != "0.0.0.0:0" && conf.Address.Port != 0)
                {
                    clientInstance.Connect(conf.Address.Address.ToString(), conf.Address.Port);
                }
                else
                {
                    ISLogger.Write("No previous server address found.");
                }


                ISLogger.Write("Service started...");
            }
            catch (Exception ex)
            {
                ISLogger.Write("LAUNCH ERROR - " + ex.Message);
                ISLogger.Write(ex.StackTrace);
                Stop();
            }
        }

        private void SaveConfig()
        {
            try
            {
                Config.Write(Config.ConfigProperty.LastConnectionState, clientInstance.IsConnected.ToString());

                if (clientInstance.ServerAddress != null && clientInstance.ServerAddress.ToString() != "0.0.0.0:0")
                    Config.Write(Config.ConfigProperty.LastConnectedAddress, clientInstance.ServerAddress.ToString());

                Config.Write(Config.ConfigProperty.LastClientName, clientInstance.ClientName);
                Config.Write(Config.ConfigProperty.LastClientGuid, clientInstance.ClientId.ToString());
                Config.Write(Config.ConfigProperty.AutoReconnectEnabled, clientInstance.AutoReconnect.ToString());
            }
            catch (ConfigurationErrorsException)
            {
                ISLogger.Write("Failed to write to settings file");
            }
        }

    

        private Process LaunchSPMain()
        {
            IntPtr sysToken = IntPtr.Zero;
            try
            {
                iHostMain?.Dispose();
                iHostMain = new AnonIpcHost("SP main");
                ISLogger.Write("Launching SP default process");
                sysToken = Token.GetSystemToken(Session.ConsoleSessionId);
                Process proc = ProcessLauncher.LaunchSP(ProcessLauncher.SPMode.Default, WindowsDesktop.Default, Settings.DEBUG_SPCONSOLEENABLED, iHostMain, sysToken);
                Token.CloseToken(sysToken);
                spMainHandle.host = iHostMain;
                spMainHandle.NotifyHandleUpdate();
                return proc;
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to launch inputshareSP main process: " + ex.Message);
                return null;
            }
            finally
            {
                if (sysToken != IntPtr.Zero)
                    Token.CloseToken(sysToken);
            }
        }

        private Process LaunchSPDragDrop()
        {
            IntPtr userToken = IntPtr.Zero;
            try
            {
                iHostDragDrop?.Dispose();
                iHostDragDrop = new AnonIpcHost("SP dragdrop");
                ISLogger.Write("Launching SP dragdrop process");
                userToken = Token.GetUserToken();
                Process proc = ProcessLauncher.LaunchSP(ProcessLauncher.SPMode.DragDrop, WindowsDesktop.Default, Settings.DEBUG_SPCONSOLEENABLED, iHostDragDrop, userToken);
                Token.CloseToken(userToken);
                spDragDropHandle.host = iHostDragDrop;
                spDragDropHandle.NotifyHandleUpdate();
                return proc;
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to launch inputshareSP dragdrop process: " + ex.Message);
                return null;
            }
            finally
            {
                if (userToken != IntPtr.Zero)
                    Token.CloseToken(userToken);
            }
        }

        
        protected override void OnStop()
        {
            try
            {
                stopping = true;
                ISLogger.Write("Inputshare service stopping...");

                ISLogger.Write("Killing child processes...");

                iHostDragDrop?.Dispose();
                iHostMain?.Dispose();

                if (ISLogger.BufferedMessages > 0)
                    Thread.Sleep(1000);

                foreach (var proc in Process.GetProcessesByName("inputsharesp"))
                    proc.Kill();

            }
            catch(Exception ex)
            {
                ISLogger.Write("An error occurred while stopping service: " + ex.Message);
                ISLogger.Write(ex.StackTrace);
                Thread.Sleep(3000);
            }
            finally
            {
                base.OnStop();
            }
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

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            ISLogger.Write("---------------------------------");
            ISLogger.Write("Unhandled exception!");
            ISLogger.Write(ex.Message);
            ISLogger.Write(ex.StackTrace);
            ISLogger.Write("---------------------------------");
            Thread.Sleep(2000);
            Stop(); 
        }

        class LoadedConfiguration
        {
            public LoadedConfiguration(bool connected, IPEndPoint address, string clientName, Guid clientGuid, bool autoReconnect)
            {
                Connected = connected;
                Address = address;
                ClientName = clientName;
                ClientGuid = clientGuid;
                AutoReconnect = autoReconnect;
            }

            public bool Connected { get; }
            public IPEndPoint Address { get; }
            public string ClientName { get; }
            public Guid ClientGuid { get; }
            public bool AutoReconnect { get; }
        }
    }
}
