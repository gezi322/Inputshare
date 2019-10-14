using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLibWindows.DragDrop;
using InputshareLibWindows.IPC.AnonIpc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InputshareSP
{
    public sealed class SPDragDropHost
    {
        private AnonIpcClient iClient;
        private WindowsDragDropManager dropMan;

        public SPDragDropHost(AnonIpcClient ipc)
        {
            ISLogger.SetLogFileName("InputshareSP_DragDropHost.log");
            dropMan = new WindowsDragDropManager();
            dropMan.Start();

            iClient = ipc;
            iClient.DoDragDropReceived += IClient_DoDragDropReceived;
            iClient.CheckForDropReceived += (object s, EventArgs e) => { dropMan.CheckForDrop(); };
            dropMan.DragDropComplete += (object s, Guid operation) => { iClient.SendDragDropComplete(operation); };
            dropMan.DragDropCancelled += (object s, Guid operation) => { iClient.SendDragDropCancelled(operation); };
            dropMan.DragDropSuccess += (object s, Guid operation) => { iClient.SendDragDropSuccess(operation); };
            dropMan.DataDropped += (object s, ClipboardDataBase data) => { iClient.SendDroppedData(data); };

            Console.Title = "SP dragdrop host";

            ISLogger.Write("Starting SP dragdrop host...");
            Console.ReadLine();
        }

        private void IClient_DoDragDropReceived(object sender, Tuple<ClipboardDataBase, Guid> e)
        {
            ISLogger.Write("Doing drop type " + e.Item1.DataType);

            ClipboardDataBase data = e.Item1;

            //If we are dropping files, we need a way for the dataobject to communicate with host
            if(data is ClipboardVirtualFileData files)
            {
                foreach (var file in files.AllFiles)
                {
                    file.ReadComplete += File_ReadComplete; ;
                    file.ReadDelegate = VirtualFile_ReadData;
                }
            }

            dropMan.DoDragDrop(e.Item1, e.Item2);
        }
        private async Task<byte[]> VirtualFile_ReadData(Guid token, Guid operationId, Guid fileId, int readLen)
        {
            try
            {
                return iClient.ReadStream(token, fileId, readLen);
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
    }
}
