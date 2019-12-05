using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.PlatformModules.Clipboard;
using InputshareLibWindows.PlatformModules.DragDrop;

namespace InputshareSP.ClipboardHost
{
    internal static class SPClipboardHost
    {
        private static AnonIpcClient iClient;
        private static WindowsClipboardManager clipMan;
        private static WindowsDragDropManager ddMan;
        private static Timer mouseStateTimer;

        internal static void Init(AnonIpcClient client)
        {
            ISLogger.SetLogFileName("InputshareSP_ClipboardHost.log");
            ISLogger.Write("Starting SP clipboard host");

            clipMan = new WindowsClipboardManager();
            clipMan.Start();

            ddMan = new WindowsDragDropManager();
            ddMan.Start();

            iClient = client;
            iClient.ClipboardDataReceived += IClient_ClipboardDataReceived;
            iClient.CheckForDropReceived += IClient_CheckForDropReceived;
            iClient.DoDragDropReceived += IClient_DoDragDropReceived;
            iClient.Disconnected += IClient_Disconnected;
            clipMan.ClipboardContentChanged += ClipMan_ClipboardContentChanged;
            ddMan.DataDropped += DdMan_DataDropped;
            ddMan.DragDropCancelled += DdMan_DragDropCancelled;
            ddMan.DragDropSuccess += DdMan_DragDropSuccess;

            mouseStateTimer = new Timer(MouseStateTimerCallback, 0, 0, 100);
        }

        private static bool lastSentMouseState = false;
        private static void MouseStateTimerCallback(object _)
        {
            bool pressed = ddMan.LeftMouseState;

            if (pressed != lastSentMouseState)
            {
                iClient.SendLeftMouseUpdate(pressed);
                lastSentMouseState = pressed;
            }
                
        }

        private static void DdMan_DragDropSuccess(object sender, EventArgs e)
        {
            iClient.SendDragDropSuccess();
        }

        private static void DdMan_DragDropCancelled(object sender, EventArgs e)
        {
            iClient.SendDragDropCancelled();
        }

        private static void DdMan_DataDropped(object sender, ClipboardDataBase data)
        {
            iClient.SendDroppedData(data);
        }

        private static void IClient_DoDragDropReceived(object sender, ClipboardDataBase data)
        {
            if (data is ClipboardVirtualFileData cbFiles)
            {
                cbFiles.RequestPartMethod = iClient.ReadStreamAsync;
                cbFiles.RequestTokenMethod = iClient.RequestFileTokenAsync;
            }

            ddMan.DoDragDrop(data);
        }

        private static void IClient_CheckForDropReceived(object sender, EventArgs e)
        {
            ddMan.CheckForDrop();
        }

        private static void IClient_Disconnected(object sender, string e)
        {
            ISLogger.Write("Lost connection to the Inputshare service... exiting");
            Exit();
        }

        private static void ClipMan_ClipboardContentChanged(object sender, ClipboardDataBase data)
        {
            iClient.SendClipboardData(data);
        }

        private static void IClient_ClipboardDataReceived(object sender, ClipboardDataBase data)
        {
            if(data is ClipboardVirtualFileData cbFiles)
            {
                cbFiles.RequestPartMethod = iClient.ReadStreamAsync;
                cbFiles.RequestTokenMethod = iClient.RequestFileTokenAsync;
            }

            clipMan.SetClipboardData(data);
        }

        private static void Exit()
        {
            iClient?.Dispose();
        }
    }
}
