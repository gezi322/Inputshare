using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Input;
using InputshareLibWindows.Clipboard;
using InputshareLibWindows.IPC.AnonIpc;
using System;
using System.Collections.Generic;
using System.Text;
using static InputshareLibWindows.Native.User32;

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

        public SPDefaultHost(AnonIpcClient ipc)
        {
            ISLogger.SetLogFileName("InputshareSP_DefaultHost.log");
            iClient = ipc;
            AssignIpcEvents();

            Console.Title = "SP default host"; 
            ISLogger.Write("Starting SP default host...");

            inputDeskThread = new InputDesktopThread();
            AssignInputThreadEvents();

            clipMan = new WindowsClipboardManager();
            clipMan.Start();
            clipMan.ClipboardContentChanged += (object s, ClipboardDataBase data) => { iClient.SendClipboardData(data); };

            Console.ReadLine();

            Exit();
        }
        
        private void AssignIpcEvents()
        {
            iClient.DisplayConfigRequested += (object s, Guid replyId) => { iClient.SendDisplayConfigReply(replyId, inputDeskThread.CurrentDisplayConfig); };
            iClient.InputReceived += (object s, ISInputData input) => { inputDeskThread.SendInput(input); };
            iClient.ClipboardDataReceived += (object s, ClipboardDataBase data) => { clipMan.SetClipboardData(data); };
        }

        private void AssignInputThreadEvents()
        {
            inputDeskThread.Start();
            inputDeskThread.DisplayConfigChanged += (object s, EventArgs _) => { iClient.SendDisplayConfigUpdate(inputDeskThread.CurrentDisplayConfig); };
            inputDeskThread.EdgeHit += (object s, Edge edge) => { iClient.SendEdgeHit(edge); };
            inputDeskThread.LeftMouseStateChanged += (object s, bool state) => { iClient.SendLeftMouseState(state); };
        }


        private void Exit()
        {
            inputDeskThread?.Stop();
            iClient.Dispose();
        }
    }
}
