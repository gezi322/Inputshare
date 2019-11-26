﻿using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Input;
using InputshareLibWindows;
using InputshareLibWindows.Clipboard;
using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.PlatformModules.Clipboard;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
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
        private HookWindow hookWnd;

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


            hookWnd = new HookWindow("Test");
            hookWnd.InitWindow();
            hookWnd.InstallClipboardMonitor(TestCallback);

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
            clipMan.Start();
            //clipMan.ClipboardContentChanged += ClipMan_ClipboardContentChanged;

            Console.ReadLine();

            Exit();
        }

        private void TestCallback(System.Windows.Forms.IDataObject obj)
        {
            ISLogger.Write("Reading native");
            System.Runtime.InteropServices.ComTypes.IDataObject native = (System.Runtime.InteropServices.ComTypes.IDataObject)obj;

            IEnumFORMATETC e=  native.EnumFormatEtc(DATADIR.DATADIR_GET);

            int index = 0;
            FORMATETC[] buff = new FORMATETC[1];
            int[] ret = new int[1];

            while (true)
            {
                e.Next(1, buff, ret);

                ISLogger.Write("Got format " + buff[0].cfFormat);

                if(ret[0] != 1)
                {
                    ISLogger.Write("Reached end");
                    break;
                }
            }
            
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
            if(e is ClipboardVirtualFileData cbFiles)
            {
                cbFiles.RequestPartMethod = iClient.ReadStreamAsync;
                cbFiles.RequestTokenMethod = iClient.RequestFileTokenAsync;
            }

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
