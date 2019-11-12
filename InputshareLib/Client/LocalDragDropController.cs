using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Net;
using InputshareLib.PlatformModules.DragDrop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InputshareLib.Client
{
    internal class LocalDragDropController
    {
        internal DragDropOperation CurrentOperation { get; private set; }

        private readonly FileAccessController fileController;
        private readonly DragDropManagerBase ddManager;
        public ISClientSocket Server { get; set; }
        private Dictionary<Guid, DragDropOperation> previousOperations = new Dictionary<Guid, DragDropOperation>();

        internal LocalDragDropController(FileAccessController fc, DragDropManagerBase dragDropManager)
        {
            ddManager = dragDropManager;
            fileController = fc;
        }

        internal void Socket_DragDropReceived(object sender, NetworkSocket.DragDropDataReceivedArgs args)
        {
            BeginReceivedOperation(args);

        }

        internal void Socket_DragDropCancelled(object sender, Guid operationId)
        {
            ddManager.CancelDrop();

            if (operationId == CurrentOperation.OperationId)
            {
                fileController.DeleteToken(CurrentOperation.AssociatedAccessToken);
            }
        }

        internal void Socket_CancelAnyDragDrop(object sender, EventArgs _)
        {
            ddManager.CancelDrop();
        }

        internal void Socket_DragDropComplete(object sender, Guid operationId)
        {
            if (operationId == CurrentOperation?.OperationId)
            {
                if (CurrentOperation.Completed)
                    return;

                CurrentOperation.Completed = true;
                fileController.DeleteToken(CurrentOperation.AssociatedAccessToken);
                if (!previousOperations.ContainsKey(CurrentOperation.OperationId))
                    previousOperations.Add(CurrentOperation.OperationId, CurrentOperation);

            }
            else if (previousOperations.ContainsKey(operationId))
            {
                if (previousOperations.TryGetValue(operationId, out DragDropOperation operation))
                {
                    fileController.DeleteToken(operation.AssociatedAccessToken);

                    operation.Completed = true;
                }
            }
            else
            {
                ISLogger.Write("LocalDragDropController: Failed to mark dragdrop operation as complete: Could not find operation ID");
            }
        }

        internal void Local_DataDropped(object sender, ClipboardDataBase cbData)
        {
            if (!Server.IsConnected)
                return;

            if (CurrentOperation != null && !previousOperations.ContainsKey(CurrentOperation.OperationId))
            {
                previousOperations.Add(CurrentOperation.OperationId, CurrentOperation);
            }

            if (Server.IsConnected)
            {
                Guid opId = Guid.NewGuid();
                CurrentOperation = new DragDropOperation(opId, cbData);
                ISLogger.Write("LocalDragDropController: Started dragdrop operation " + opId);
                Server.SendDragDropData(cbData.ToBytes(), opId);
            }
        }

        internal void Local_DragDropSuccess(object sender, Guid id)
        {
            if (!Server.IsConnected)
                return;

            Server?.NotifyDragDropSuccess(id, true);
        }

        internal void Local_DragDropComplete(object sender, Guid operationId)
        {
            if (!Server.IsConnected)
                return;

            Server?.SendDragDropComplete(operationId);
        }

        internal void Local_DragDropCancelled(object sender, Guid operationId)
        {
            if (!Server.IsConnected)
                return;

            Server?.NotifyDragDropSuccess(operationId, false);
        }

        private async void BeginReceivedOperation(NetworkSocket.DragDropDataReceivedArgs args)
        {
            //Check if the received operation has previously been received
            if (CurrentOperation?.OperationId == args.OperationId)
            {
                ddManager.DoDragDrop(CurrentOperation.Data, args.OperationId);
                return;
            }

            if (CurrentOperation != null && !previousOperations.ContainsKey(CurrentOperation.OperationId))
                previousOperations.Add(CurrentOperation.OperationId, CurrentOperation);


            ClipboardDataBase cbData = ClipboardDataBase.FromBytes(args.RawData);


            //We need to setup the virtual files if this is a file drop
            if (cbData is ClipboardVirtualFileData cbFiles)
            {
                //Get an access token for the files from the server
                Guid token;
                try
                {
                    token = await Server.RequestFileTokenAsync(args.OperationId);
                }
                catch (Exception ex)
                {
                    ISLogger.Write("LocalDragDropController: Failed to get access token for dragdrop operation: " + ex.Message);
                    return;
                }


                for (int i = 0; i < cbFiles.AllFiles.Count; i++)
                {
                    cbFiles.AllFiles[i].RemoteAccessToken = token;
                    cbFiles.AllFiles[i].ReadComplete += VirtualFile_ReadComplete;
                    cbFiles.AllFiles[i].ReadDelegate = VirtualFile_RequestDataAsync;
                    cbFiles.AllFiles[i].FileOperationId = args.OperationId;
                }
            }

            CurrentOperation = new DragDropOperation(args.OperationId, cbData);
            ddManager.DoDragDrop(cbData, args.OperationId);
        }

        private void VirtualFile_ReadComplete(object sender, EventArgs e)
        {
            if (!Server.IsConnected)
                return;

            ClipboardVirtualFileData.FileAttributes file = sender as ClipboardVirtualFileData.FileAttributes;
            Server.RequestCloseStream(file.RemoteAccessToken, file.FileRequestId);
        }

        private async Task<byte[]> VirtualFile_RequestDataAsync(Guid token, Guid operationId, Guid fileId, int readLen)
        {
            return await Server.RequestReadStreamAsync(token, fileId, readLen);
        }

        internal class DragDropOperation
        {
            public DragDropOperation(Guid operationId, ClipboardDataBase data)
            {
                OperationId = operationId;
                Data = data;
            }

            public bool Completed { get; set; }
            public Guid AssociatedAccessToken { get; set; }
            public Guid OperationId { get; }
            public ClipboardDataBase Data { get; }
        }
    }
}
