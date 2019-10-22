using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Input;
using InputshareLibWindows.Clipboard;
using InputshareLibWindows.IPC.AnonIpc;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareSP
{
    /// <summary>
    /// Runs inputshareSP in the default mode.
    /// Controls everything within user space except for drag/drop, which needs to be run under user priviliges
    /// </summary>
    public sealed class SPDefaultHost
    {
        private AnonIpcClient iClient;
        private InputDesktopThread inputDeskThread;
        private WindowsClipboardManager clipMan;

        public SPDefaultHost(string readPipe, string writePipe)
        {
            Console.Title = "SP default host";
            

            ISLogger.Write("Connecting to service...");
            iClient = new AnonIpcClient(readPipe, writePipe, "ServiceConnection");

            Task.Run(() => {
                Thread.Sleep(5000);

                if (!iClient.IsConnected)
                {
                    ISLogger.Write("Failed to connect to service... exiting");
                    Exit();
                }
            });

            ISLogger.Write("Starting SP default host...");

            iClient.Connected += IClient_Connected;
            iClient.Disconnected += IClient_Disconnected;
            iClient.InputReceived += IClient_InputReceived;
            iClient.ClipboardDataReceived += IClient_ClipboardDataReceived;
            iClient.DisplayConfigRequested += IClient_DisplayConfigRequested;

            inputDeskThread = new InputDesktopThread();
            
            inputDeskThread.Start();
            
            AssignInputThreadEvents();
            

            clipMan = new WindowsClipboardManager();
            clipMan.ClipboardContentChanged += ClipMan_ClipboardContentChanged;
            clipMan.Start();

            Console.ReadLine();

            Exit();
        }

        private void ClipMan_ClipboardContentChanged(object sender, ClipboardDataBase e)
        {
            iClient.SendClipboardData(e);
        }

        private void IClient_Disconnected(object sender, string reason)
        {
            ISLogger.Write("Lost connection to service... " + reason);
            Exit();
        }

        private void IClient_Connected(object sender, EventArgs e)
        {
            ISLogger.Write("Connected to service");
        }

        private void IClient_ClipboardDataReceived(object sender, ClipboardDataBase e)
        {
            if (clipMan != null && clipMan.Running)
                clipMan.SetClipboardData(e);
        }

        private void IClient_InputReceived(object sender, ISInputData e)
        {
            if (inputDeskThread != null && inputDeskThread.Running)
                inputDeskThread.SendInput(e);
        }

        private void IClient_DisplayConfigRequested(object sender, AnonIpcClient.DisplayConfigRequestedArgs e)
        {
            e.Config = InputDesktopThread.GetDisplayConfig();
        }

        private void AssignInputThreadEvents()
        {
            inputDeskThread.DisplayConfigChanged += InputDeskThread_DisplayConfigChanged;
            inputDeskThread.EdgeHit += InputDeskThread_EdgeHit;
            inputDeskThread.LeftMouseStateChanged += InputDeskThread_LeftMouseStateChanged;
        }

        private void InputDeskThread_LeftMouseStateChanged(object sender, bool e)
        {
            iClient.SendLeftMouseUpdate(e);
        }

        private void InputDeskThread_EdgeHit(object sender, Edge e)
        {
            iClient.SendEdgeHit(e);
        }

        private void InputDeskThread_DisplayConfigChanged(object sender, EventArgs e)
        {
            iClient.SendDisplayConfig(inputDeskThread.CurrentDisplayConfig);
        }

        private void Exit()
        {
            inputDeskThread?.Stop();
            Process.GetCurrentProcess().Kill();
        }
    }
}
