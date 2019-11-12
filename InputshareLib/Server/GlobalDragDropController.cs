using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Net;
using InputshareLib.PlatformModules.DragDrop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InputshareLib.Server
{

    /// <summary>
    /// Controls global drag/drop operations between clients/server
    /// </summary>
    class GlobalDragDropController
    {
        private readonly ClientManager clientMan;
        private readonly DragDropManagerBase ddManager;
        private readonly FileAccessController fileController;

        private readonly Dictionary<Guid, DragDropOperation> previousOperationIds = new Dictionary<Guid, DragDropOperation>();
        public DragDropOperation CurrentOperation { get; private set; }
        private ISServerSocket currentInputClient = ISServerSocket.Localhost;
        public GlobalDragDropController(ClientManager clientManager, DragDropManagerBase dropManager, FileAccessController fileAccessController)
        {
            clientMan = clientManager;
            fileController = fileAccessController;
            ddManager = dropManager;

            if (!ddManager.Running)
                ddManager.Start();

            ddManager.DataDropped += DdManager_DataDropped;
            ddManager.DragDropCancelled += DdManager_DragDropCancelled;
            ddManager.DragDropComplete += DdManager_DragDropComplete;
            ddManager.DragDropSuccess += DdManager_DragDropSuccess;
        }

        private void DdManager_DragDropSuccess(object sender, Guid operationId)
        {
            OnDropSuccess(ISServerSocket.Localhost, operationId);
        }

        private void DdManager_DragDropComplete(object sender, Guid operationId)
        {
            OnDropComplete(ISServerSocket.Localhost, operationId);
        }

        private void DdManager_DragDropCancelled(object sender, Guid operationId)
        {
            OnDropCancel(ISServerSocket.Localhost, operationId);
        }

        private void DdManager_DataDropped(object sender, ClipboardDataBase cbData)
        {
            BeginOperation(ISServerSocket.Localhost, cbData, Guid.NewGuid());
        }

        public void Client_RequestCloseStream(object sender, NetworkSocket.RequestCloseStreamArgs args)
        {
            if (CurrentOperation?.RemoteFileAccessToken == args.Token)
            {
                CurrentOperation.Host.RequestCloseStream(args.Token, args.File);
            }
            else
            {
                foreach (var operation in previousOperationIds.Values)
                {
                    if (operation.RemoteFileAccessToken == args.Token)
                    {
                        operation.Host.RequestCloseStream(args.Token, args.File);
                    }
                }
            }
        }

        public void Client_DataDropped(object sender, NetworkSocket.DragDropDataReceivedArgs args)
        {
            ISServerSocket client = sender as ISServerSocket;
            BeginOperation(client, ClipboardDataBase.FromBytes(args.RawData), args.OperationId);
        }

        public void Client_DragDropCancelled(object sender, Guid operationId)
        {
            ISServerSocket client = sender as ISServerSocket;
            OnDropCancel(client, operationId);
        }

        public void Client_DragDropSuccess(object sender, Guid operationId)
        {
            ISServerSocket client = sender as ISServerSocket;
            OnDropSuccess(client, operationId);
        }

        public void Client_DragDropComplete(object sender, Guid operationId)
        {
            ISServerSocket client = sender as ISServerSocket;
            OnDropComplete(client, operationId);
        }

        private async void BeginOperation(ISServerSocket sender, ClipboardDataBase cbData, Guid operationId)
        {
            if (!ddManager.Running)
                ddManager.Start();

            if (cbData == null)
            {
                ISLogger.Write("DragDropController: Cannot begin operation: Data was null");
                return;
            }

            if (CurrentOperation != null && CurrentOperation.State == DragDropState.Dragging)
            {
                ISLogger.Write("DragDropController: Cannot begin operation: another operation is currently dragging");
                return;
            }

            ISLogger.Write("New DragDrop operation. Type {0}, Host {1}, ID {2}", cbData.DataType, sender.ClientName, operationId);

            if (cbData.DataType == ClipboardDataType.File)
            {

                await BeginFileOperationAsync(cbData as ClipboardVirtualFileData, sender, operationId);
                OnOperationChanged();
                return;
            }
            else
            {
                BeginTextOrImageOperation(cbData, operationId);
                OnOperationChanged();
            }
        }

        /// <summary>
        /// Begins a file dragdrop operation, and assigns an access token to the operation
        /// </summary>
        /// <param name="cbFiles"></param>
        /// <param name="host"></param>
        /// <param name="operationId"></param>
        private async Task BeginFileOperationAsync(ClipboardVirtualFileData cbFiles, ISServerSocket host, Guid operationId)
        {
            Guid fileAccesstoken = Guid.Empty;

            DragDropOperation newOperation = new DragDropOperation(cbFiles, host, operationId);

            try
            {
                fileAccesstoken = await CreateAccessTokenForOperation(newOperation);
            }
            catch (Exception ex)
            {
                ISLogger.Write("DragDropController: Cancelling operation, could not generate file access token: " + ex.Message);
                return;
            }
            newOperation.RemoteFileAccessToken = fileAccesstoken;

            //Create events, incase the files are dropped on localhost
            if (!newOperation.Host.IsLocalhost)
            {
                for (int i = 0; i < cbFiles.AllFiles.Count; i++)
                {
                    cbFiles.AllFiles[i].RemoteAccessToken = fileAccesstoken;
                    cbFiles.AllFiles[i].ReadDelegate = File_RequestDataAsync;
                    cbFiles.AllFiles[i].ReadComplete += VirtualFile_ReadComplete;
                    cbFiles.AllFiles[i].FileOperationId = newOperation.OperationId;
                }
            }

            //If the previous dragdrop operation is still transfering files, store it so that the files can keep being transfered
            if (CurrentOperation != null && CurrentOperation.State == DragDropState.TransferingFiles)
                previousOperationIds.Add(CurrentOperation.OperationId, CurrentOperation);

            //Sets the new operation, which is automatically set to dragging state
            CurrentOperation = newOperation;
            ISLogger.Write("BeginFileOperation " + host.ClientName + " - " + operationId);
            if (currentInputClient.IsLocalhost)
            {
                ddManager.DoDragDrop(CurrentOperation.OperationData, newOperation.OperationId);
            }
            else
            {
                currentInputClient.SendDragDropData(CurrentOperation.OperationData.ToBytes(), CurrentOperation.OperationId);
            }
        }

        /// <summary>
        /// If localhost drops files, each virtualfile will be given this delegate to allow it to read data 
        /// from the operation host. The virtual file will contain the token that it should use to access the files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VirtualFile_ReadComplete(object sender, EventArgs e)
        {
            ClipboardVirtualFileData.FileAttributes file = sender as ClipboardVirtualFileData.FileAttributes;
            ISLogger.Write("DragDropController: File {0} read", file.FileName);

            if (CurrentOperation.RemoteFileAccessToken != file.RemoteAccessToken)
            {
                if (previousOperationIds.TryGetValue(file.FileOperationId, out DragDropOperation operation))
                    operation.Host.RequestCloseStream(file.RemoteAccessToken, file.FileRequestId);
                else
                    ISLogger.Write("DragDropController: Error sending stream close for file {0}: cannot find dragdrop operation", file.FileName);
            }
            else
            {
                CurrentOperation.Host.RequestCloseStream(file.RemoteAccessToken, file.FileRequestId);
            }
        }

        private void BeginTextOrImageOperation(ClipboardDataBase cbData, Guid operationId)
        {
            //If the previous dragdrop operation is still transfering files, store it so that the files can keep being transfered
            if (CurrentOperation != null && CurrentOperation.State == DragDropState.TransferingFiles)
                previousOperationIds.Add(CurrentOperation.OperationId, CurrentOperation);


            //Create a new operation with the text/image data, we don't care about the ID or host
            //as text and image data is not streamed the same way as file data. Text/Image data are sent as a single block
            CurrentOperation = new DragDropOperation(cbData, null, operationId);

            if (currentInputClient.IsLocalhost)
            {
                ddManager.DoDragDrop(cbData, operationId);
            }
            else
            {
                ISLogger.Write("SDragDropController: Sending dragdrop type {0} to {1}", cbData.DataType, currentInputClient.ClientName);
                currentInputClient.SendDragDropData(cbData.ToBytes(), CurrentOperation.OperationId);
            }


        }

        private void OnDropCancel(ISServerSocket sender, Guid operationId)
        {
            ISLogger.Write("OnDropCancel " + sender.ClientName + " - " + operationId);

            if (sender != currentInputClient)
            {
                ISLogger.Write("DragDropController: {0} attempted to cancel dragdrop operation when they are not input client!", sender.ClientName);
                return;
            }


            DragDropOperation operation;

            if (operationId == CurrentOperation?.OperationId)
                operation = CurrentOperation;
            else if (!previousOperationIds.TryGetValue(operationId, out operation))
            {
                ISLogger.Write("DragDropController: Error cancelling operation: operation not found");
                return;
            }

            if (operation.State != DragDropState.Dragging)
            {
                //ISLogger.Write("DragDropController: {0} attempted to cancel dragdrop operation when state is {1}", sender.ClientName, operation.State);
                return;
            }

            ISLogger.Write("DragDropController: {0} cancelled current operation {1}", sender.ClientName, operation.OperationId);
            operation.State = DragDropState.Cancelled;

            if (operation.DataType == ClipboardDataType.File)
            {
                if (operation.Host != null && operation.Host.IsLocalhost)
                    fileController.DeleteToken(operation.RemoteFileAccessToken);
                else if (operation.Host != null && !operation.Host.IsLocalhost)
                    CurrentOperation.Host.SendDragDropComplete(operationId);
            }

            CancelAllDragDrops();

            OnOperationChanged();
        }

        private void OnDropSuccess(ISServerSocket sender, Guid operationId)
        {
            ISLogger.Write("OnDropSuccess " + sender.ClientName + " - " + operationId);
            if (sender != currentInputClient)
            {
                ISLogger.Write("DragDropController: {0} attempted to send drop success when they are not input client!", sender.ClientName);
                return;
            }


            //If the specified operation is the current operation, mark it as success only if it is in the dragging state
            if (CurrentOperation?.OperationId == operationId && CurrentOperation?.State == DragDropState.Dragging)
            {
                ISLogger.Write("DragDropController: {0} marked current dragdrop operation success... {1}", sender.ClientName, CurrentOperation.OperationId);
                CurrentOperation.State = DragDropState.TransferingFiles;
                CurrentOperation.ReceiverClient = sender;
                OnOperationChanged();
            }
            else if (CurrentOperation.OperationId == operationId && CurrentOperation.State != DragDropState.Dragging)
            {
                //TODO - WindowsDragDropManager reports filedrop success twice
                ISLogger.Write("DragDropController: {0} attempted to send drop success when specified operationstate is {1}!", sender.ClientName, CurrentOperation.State);
                return;
            }

            //If it is not the current operation, check if it a previous operation, if so mark as transferingfiles
            if (CurrentOperation?.OperationId != operationId)
            {
                if (previousOperationIds.TryGetValue(operationId, out DragDropOperation operation))
                {
                    if (operation.State == DragDropState.Dragging)
                    {
                        ISLogger.Write("DragDropController: {0} marked previous dragdrop operation success {1}", sender.ClientName, operation.OperationId);
                        operation.State = DragDropState.TransferingFiles;
                        operation.ReceiverClient = sender;
                        OnOperationChanged();
                        return;
                    }
                }
                else
                {
                    ISLogger.Write("DragDropController: {0} attempted to mark unknown dragdrop operation as success", sender.ClientName);
                    return;
                }
            }

            CurrentOperation.State = DragDropState.TransferingFiles;
        }

        public DragDropOperation GetOperationFromToken(Guid tokenId)
        {
            if (CurrentOperation.RemoteFileAccessToken == tokenId)
                return CurrentOperation;

            foreach (var op in previousOperationIds)
                if (op.Value.RemoteFileAccessToken == tokenId)
                    return op.Value;

            return null;
        }

        /// <summary>
        /// Called when localhost or a client fully completes a dragdrop operation
        /// Complete means that the files have been fully read, while success means they have been dropped.
        /// When the drop is complete, we can then close all filestreams associated with this drop.
        /// Also if the host is a client, let them know that the drop is complete.
        /// </summary>
        /// <param name="sender"></param>
        private void OnDropComplete(ISServerSocket sender, Guid operationId)
        {
            ISLogger.Write("OnDropComplete " + sender.ClientName + " - " + operationId);

            if (CurrentOperation.OperationId == operationId)
            {
                ISLogger.Write("DragDropController: Current dragdrop operation marked as complete by " + sender.ClientName + " " + operationId);

                if (CurrentOperation?.State == DragDropState.Cancelled)
                    return;

                CurrentOperation.State = DragDropState.Complete;

                //Let the host know that it can now close all streams and delete access token
                if (!CurrentOperation.Host.IsLocalhost)
                    CurrentOperation.Host.SendDragDropComplete(operationId);
                else
                    fileController.DeleteToken(CurrentOperation.RemoteFileAccessToken);
                OnOperationChanged();
            }
            else
            {
                if (previousOperationIds.TryGetValue(operationId, out DragDropOperation oldOperation))
                {
                    if (oldOperation?.State == DragDropState.Cancelled)
                        return;

                    oldOperation.State = DragDropState.Complete;
                    ISLogger.Write("DragDropController: " + sender.ClientName + " marked old dragdrop operation as complete " + operationId);

                    //Let the host know that it can now close all streams and delete access token
                    //or delete the access tokens if localhost is the host
                    if (!CurrentOperation.Host.IsLocalhost)
                        CurrentOperation.Host.SendDragDropComplete(operationId);
                    else
                        fileController.DeleteToken(oldOperation.RemoteFileAccessToken);

                    previousOperationIds.Remove(operationId);

                    OnOperationChanged();
                    return;
                }
                ISLogger.Write(sender.ClientName + " attempted to mark incorrect dragdrop operation as complete - " + operationId);
            }

        }

        /// <summary>
        /// Cancels any drop operations currently running on any client
        /// </summary>
        private void CancelAllDragDrops(ISServerSocket exceptClient = null)
        {
            ddManager.CancelDrop();
            foreach (var client in clientMan.AllClients.Where(i => !i.IsLocalhost))
            {
                if (exceptClient != null && client == exceptClient)
                    continue;

                client.SendCancelDragDrop();
            }
        }

        public void HandleClientSwitch(ISServerSocket newClient, ISServerSocket oldClient)
        {
            currentInputClient = newClient;

            //if moving from localhost to client, check for drop if not in operation, Only do this if the left mouse button is down
            if (oldClient.IsLocalhost && CurrentOperation?.State != DragDropState.Dragging && ddManager.LeftMouseState)
                ddManager.CheckForDrop();

            ddManager.CancelDrop();

            if (CurrentOperation == null)
                return;

            //If we are dragging a file, make sure that we don't send the dragdrop back to the host
            //as this causes issues
            if (CurrentOperation.DataType == ClipboardDataType.File && CurrentOperation.State == DragDropState.Dragging)
                if (newClient == CurrentOperation.Host)
                {
                    ISLogger.Write("Dragdrop returned to sender... cancelling");
                    OnDropCancel(newClient, CurrentOperation.OperationId);
                    return;
                }

            if (CurrentOperation.State == DragDropState.Dragging)
                if (newClient.IsLocalhost)
                    ddManager.DoDragDrop(CurrentOperation.OperationData, CurrentOperation.OperationId);

            //if we are dragging a file, send the dragdrop data to the client
            if (CurrentOperation.State == DragDropState.Dragging)
                if (!newClient.IsLocalhost)
                    newClient.SendDragDropData(CurrentOperation.OperationData.ToBytes(), CurrentOperation.OperationId);
        }

        /// <summary>
        /// The virtual file object calls this method when it needs to read the next block of data
        /// </summary>
        /// <param name="token"></param>
        /// <param name="fileId"></param>
        /// <param name="readLen"></param>
        /// <returns></returns>
        private async Task<byte[]> File_RequestDataAsync(Guid token, Guid operationId, Guid fileId, int readLen)
        {
            DragDropOperation operation;

            if (operationId == CurrentOperation.OperationId)
                operation = CurrentOperation;
            else if (previousOperationIds.ContainsKey(operationId))
                previousOperationIds.TryGetValue(operationId, out operation);
            else
            {
                ISLogger.Write("DragDropController: Operation ID not found");
                return new byte[0];
            }

            try
            {
                return await operation.Host.RequestReadStreamAsync(token, fileId, readLen);
            }
            catch (NetworkSocket.RequestTimedOutException)
            {
                ISLogger.Write("DragDropController: Failed to read from remote file: Request timed out");
                return new byte[0];
            }
            catch (Exception ex)
            {
                ISLogger.Write("DragDropController: Failed to read from remote file: " + ex.Message);
                return new byte[0];
            }
        }

        private async Task<Guid> CreateAccessTokenForOperation(DragDropOperation operation)
        {
            if (operation.Host.IsLocalhost)
            {
                return GenerateLocalAccessTokenForOperation(operation);
            }
            else
            {
                return await operation.Host.RequestFileTokenAsync(operation.OperationId);
            }

        }

        private void OnOperationChanged()
        {

        }

        private Guid GenerateLocalAccessTokenForOperation(DragDropOperation operation)
        {
            if (operation.DataType != ClipboardDataType.File)
            {
                throw new ArgumentException("DateType must be file");
            }

            ClipboardVirtualFileData file = operation.OperationData as ClipboardVirtualFileData;
            Guid[] fIds = new Guid[file.AllFiles.Count];
            string[] fSources = new string[file.AllFiles.Count];

            for (int i = 0; i < file.AllFiles.Count; i++)
            {
                fIds[i] = file.AllFiles[i].FileRequestId;
                fSources[i] = file.AllFiles[i].FullPath;
            }

            return fileController.CreateFileReadTokenForGroup(new FileAccessController.FileAccessInfo(fIds, fSources), 10000);
        }

        internal class DragDropOperation
        {
            public DragDropOperation(ClipboardDataBase operationData, ISServerSocket host, Guid operationId)
            {
                OperationData = operationData;
                Host = host;
                OperationId = operationId;
                StartTime = DateTime.Now;
            }
            public DateTime StartTime { get; }
            public ClipboardDataType DataType { get => OperationData.DataType; }
            public ClipboardDataBase OperationData { get; }
            public ISServerSocket Host { get; }
            public Guid OperationId { get; }
            public Guid RemoteFileAccessToken { get; set; }
            public ISServerSocket ReceiverClient { get; set; }
            public DragDropState State { get; set; } = DragDropState.Dragging;
        }

        public enum DragDropState
        {
            None,
            Dragging,
            Dropped,
            Complete,
            TransferingFiles,
            Cancelled
        }
    }
}
