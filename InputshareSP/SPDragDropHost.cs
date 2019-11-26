using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.PlatformModules.DragDrop;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareSP
{
    public sealed class SPDragDropHost
    {
        private AnonIpcClient iClient;
        private WindowsDragDropManager dropMan;

        public SPDragDropHost(string readPipe, string writePipe)
        {
            dropMan = new WindowsDragDropManager();
            dropMan.Start();

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

            iClient.DoDragDropReceived += IClient_DoDragDropReceived;
            iClient.CheckForDropReceived += (object s, EventArgs e) => { ISLogger.Write("Checking for drop..."); dropMan.CheckForDrop(); };
            iClient.Disconnected += IClient_Disconnected;
            iClient.Connected += IClient_Connected;

            dropMan.DragDropCancelled += (object s, EventArgs e) => { iClient.SendDragDropCancelled(); };
            dropMan.DragDropSuccess += (object s, EventArgs e) => { iClient.SendDragDropSuccess(); };
            dropMan.DataDropped += (object s, ClipboardDataBase data) => { iClient.SendDroppedData(data); };

            Console.Title = "SP dragdrop host";
           
            ISLogger.Write("Starting SP dragdrop host...");
            Console.ReadLine();
        }

        private void IClient_Connected(object sender, EventArgs e)
        {
            ISLogger.Write("Connected to service");
        }

        private void IClient_Disconnected(object sender, string reason)
        {
            ISLogger.Write("Lost connection to service... " + reason);
            Thread.Sleep(500);
            Exit();
        }

        private void IClient_DoDragDropReceived(object sender, Tuple<Guid, ClipboardDataBase> ret)
        {
            ISLogger.Write("Doing drop type " + ret.Item2.DataType);

            ISLogger.Write("Drop data operation = " + ret.Item1);
            ClipboardDataBase data = ret.Item2;

            if(data is ClipboardVirtualFileData cbFiles)
            {
                cbFiles.OperationId = ret.Item1;
                cbFiles.RequestTokenMethod = iClient.RequestFileTokenAsync;
                cbFiles.RequestPartMethod = iClient.ReadStreamAsync;
            }

            dropMan.DoDragDrop(ret.Item2);
        }

        private void Exit()
        {
            if (dropMan.Running)
                dropMan.Stop();

            Process.GetCurrentProcess().Kill();
        }
    }
}
