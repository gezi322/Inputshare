using InputshareLib;
using InputshareLib.Client;
using InputshareLibWindows;
using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.IPC.NetIpc;
using InputshareLibWindows.Windows;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareService
{
    public sealed class InputshareService : ServiceBase
    {
        private ISClient clientInstance;

        private AnonIpcHost iHostMain;
        private AnonIpcHost iHostClipboard;
        private NetIpcHost appHost;

        private IpcHandle spMainHandle = new IpcHandle();
        private IpcHandle spClipboardHandle = new IpcHandle();

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
            ISLogger.Write("----------------------------------------------");
            ISLogger.Write("Inputshare service starting...");
            ISLogger.Write("Console session state: " + Session.ConsoleSessionState);
            Config.LoadFile();
            ISLogger.Write("Loaded config");

            SetPriority();
            
           
            Task.Run(() => { SpDragDropTaskLoop(); });
            Task.Run(() => { SpMainTaskLoop(); });

            Thread.Sleep(500);
            Task.Run(() => { LoadAndStart(); });


            base.OnStart(args);
        }

        private void SpDragDropTaskLoop()
        {
            iHostClipboard = new AnonIpcHost("Dragdrop process");
            spClipboardHandle.host = iHostClipboard;

            while (!stopping)
            {
                try
                {
                    while (!Session.ConsoleSessionLoggedIn)
                        Thread.Sleep(500);

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
            spMainHandle.host = iHostMain;

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


        private void ClientInstance_Disconnected(object sender, EventArgs e)
        {

        }

        private void ClientInstance_Connected(object sender, System.Net.IPEndPoint e)
        {
            Config.TryWrite(ServiceConfigProperties.LastConnectedAddress, e.ToString());
        }

        private void ClientInstance_ConnectionFailed(object sender, string error)
        {

        }

        private void ClientInstance_ConnectionError(object sender, string error)
        {

        }

        private void LoadAndStart()
        {
            try
            {
                clientInstance = new ISClient(WindowsDependencies.GetServiceDependencies(spMainHandle, spClipboardHandle), new StartOptions(new System.Collections.Generic.List<string>()));
                

                clientInstance.ConnectionError += ClientInstance_ConnectionError;
                clientInstance.ConnectionFailed += ClientInstance_ConnectionFailed;
                clientInstance.Connected += ClientInstance_Connected;
                clientInstance.SasRequested += (object a, EventArgs b) => InputshareLibWindows.Native.Sas.SendSAS(false);
                clientInstance.Disconnected += ClientInstance_Disconnected;
                clientInstance.AutoReconnect = true;

                try
                {
                    appHost = new NetIpcHost(clientInstance, "App connection");
                }catch(Exception ex)
                {
                    ISLogger.Write("Failed to create NetIPC host: " + ex.Message);
                }

                if(Config.TryReadProperty(ServiceConfigProperties.LastConnectedAddress, out string addrStr)){
                    if (IPEndPoint.TryParse(addrStr, out IPEndPoint addr))
                    {
                        clientInstance.Connect(addr.Address.ToString(), addr.Port);
                    }
                    else
                    {
                        ISLogger.Write("Invalid address in config");
                    }
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

        private Process LaunchSPMain()
        {
            IntPtr sysToken = IntPtr.Zero;
            try
            {
                iHostMain?.Dispose();
                iHostMain = new AnonIpcHost("SP main");
                ISLogger.Write("Launching SP default process");


                if(Settings.DEBUG_SPECIFYSPSESSION != -1)
                    sysToken = unchecked(Token.GetSystemToken((uint)Settings.DEBUG_SPECIFYSPSESSION));
                else
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
                iHostClipboard?.Dispose();
                iHostClipboard = new AnonIpcHost("SP dragdrop");
                ISLogger.Write("Launching SP dragdrop process");
                userToken = Token.GetUserToken();
                Process proc = ProcessLauncher.LaunchSP(ProcessLauncher.SPMode.Clipboard, WindowsDesktop.Default, Settings.DEBUG_SPCONSOLEENABLED, iHostClipboard, userToken);
                Token.CloseToken(userToken);
                spClipboardHandle.host = iHostClipboard;
                spClipboardHandle.NotifyHandleUpdate();
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

                iHostClipboard?.Dispose();
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
    }
}
