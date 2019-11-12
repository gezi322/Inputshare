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

            dropMan.DragDropComplete += (object s, Guid operation) => { iClient.SendDragDropComplete(operation); };
            dropMan.DragDropCancelled += (object s, Guid operation) => { iClient.SendDragDropCancelled(operation); };
            dropMan.DragDropSuccess += (object s, Guid operation) => { iClient.SendDragDropSuccess(operation); };
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

            ClipboardDataBase data = ret.Item2;

            //If we are dropping files, we need a way for the dataobject to communicate with host
            if( data is ClipboardVirtualFileData files)
            {
                foreach (var file in files.AllFiles)
                {
                    file.ReadComplete += File_ReadComplete; ;
                    file.ReadDelegate = VirtualFile_ReadData;
                }
            }

            dropMan.DoDragDrop(ret.Item2, ret.Item1);
        }
        private async Task<byte[]> VirtualFile_ReadData(Guid token, Guid operationId, Guid fileId, int readLen)
        {
            try
            {
                return await iClient.ReadStreamAsync(token, fileId, readLen);
            }catch(Exception ex)
            {
                ISLogger.Write("Failed to read external data stream: " + ex.Message);
                ISLogger.Write(ex.Source);
                ISLogger.Write(ex.StackTrace);
                return new byte[0];
            }

            
        }

        private void File_ReadComplete(object sender, EventArgs e)
        {

        }

        private void Exit()
        {
            if (dropMan.Running)
                dropMan.Stop();

            Process.GetCurrentProcess().Kill();
        }
    }
}
