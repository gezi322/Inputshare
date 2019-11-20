using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using InputshareLib.Clipboard;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Net;
using InputshareLib.PlatformModules.DragDrop;

namespace InputshareLib.Server
{
    internal class GlobalDragDropController
    {
        public ServerDragDropDataOperation CurrentOperation { get; private set; }

        private bool OperationNull { get => CurrentOperation == null; }
        private readonly ClientManager clientMan;
        private readonly DragDropManagerBase ddManager;
        private readonly Dictionary<Guid, ServerDragDropDataOperation> previousOperations = new Dictionary<Guid, ServerDragDropDataOperation>();
        private ISServerSocket currentActiveClient;

        public GlobalDragDropController(DragDropManagerBase dropMan, ClientManager clientManager)
        {
            clientMan = clientManager;
            ddManager = dropMan;

            ddManager.DataDropped += OnLocalDataDropped;
            ddManager.DragDropCancelled += OnLocalDropCancelled;
            ddManager.DragDropSuccess += OnLocalDropSuccess;
        }

        private void OnLocalDropSuccess(object sender, EventArgs _)
        {
            SetStateAsClient(ServerDragDropDataOperation.DragDropState.Dropped, ISServerSocket.Localhost);
        }

        private void OnLocalDropCancelled(object sender, EventArgs _)
        {
            SetStateAsClient(ServerDragDropDataOperation.DragDropState.Cancelled, ISServerSocket.Localhost);
        }

        private void OnLocalDataDropped(object sender, ClipboardDataBase data)
        {
           BeginOperation(data, ISServerSocket.Localhost, Guid.Empty);
        }

        public void OnClientDataDropped(object sender, NetworkSocket.DragDropDataReceivedArgs args)
        {
            BeginOperation(ClipboardDataBase.FromBytes(args.RawData), sender as ISServerSocket, args.OperationId);
        }

        public void OnClientDropCancelled(object sender, EventArgs _)
        {
            SetStateAsClient(ServerDragDropDataOperation.DragDropState.Cancelled, sender as ISServerSocket);
        }

        public void OnClientDropSuccess(object sender, EventArgs _)
        {
            SetStateAsClient(ServerDragDropDataOperation.DragDropState.Dropped, sender as ISServerSocket);
        }

        private void SetStateAsClient(ServerDragDropDataOperation.DragDropState state, ISServerSocket client)
        {
            if (OperationNull)
                return;

            if(CurrentOperation.TargetClient != client)
            {
                ISLogger.Write("Client {0} tried to set dragdrop operation state to {1} when they are not the target client", client, state);
                return;
            }

            SetState(state);
            ISLogger.Write("Client {0} set dragdrop state to {1}", client, state);
        }

        private void SetState(ServerDragDropDataOperation.DragDropState state)
        {
            if(state == ServerDragDropDataOperation.DragDropState.Dropped)
                if (CurrentOperation.Data.DataType == ClipboardDataType.File)
                    CurrentOperation.State = ServerDragDropDataOperation.DragDropState.TransferingFiles;
                else
                    CurrentOperation.State = ServerDragDropDataOperation.DragDropState.Complete;

            if(state == ServerDragDropDataOperation.DragDropState.Cancelled)
            {
                ddManager.CancelDrop();

                foreach(var client in clientMan.AllClients)
                {
                    client.SendCancelDragDrop();
                }
            }

            CurrentOperation.State = state;
        }

        private void BeginOperation(ClipboardDataBase data, ISServerSocket host, Guid operationId)
        {
            if (operationId != Guid.Empty)
                CurrentOperation = new ServerDragDropDataOperation(data, host, operationId);
            else
                CurrentOperation = new ServerDragDropDataOperation(data, host);

            CurrentOperation.TargetClient = currentActiveClient;
            CurrentOperation.State = ServerDragDropDataOperation.DragDropState.Dragging;

            BroadcastOperation();
        }

        private void BroadcastOperation()
        {
            if (CurrentOperation.TargetClient.IsLocalhost)
                DoLocalDragDrop();
            else
                CurrentOperation.TargetClient.SendDragDropData(CurrentOperation.Data.ToBytes(), CurrentOperation.OperationGuid);
        }

        private void DoLocalDragDrop()
        {
            if(CurrentOperation.Data is ClipboardVirtualFileData cbFiles)
            {
                cbFiles.RequestPartMethod = CurrentOperation.Host.RequestReadStreamAsync;
                cbFiles.RequestTokenMethod = CurrentOperation.Host.RequestFileTokenAsync;
            }

            ddManager.DoDragDrop(CurrentOperation.Data, CurrentOperation.OperationGuid);
        }

        public void HandleClientSwitchAsync(ISServerSocket oldActiveClient, ISServerSocket newActiveClient)
        {
            currentActiveClient = newActiveClient;
            ISLogger.Write("Handle client switch {0}=>{1}", oldActiveClient, newActiveClient);

            if (CurrentOperation != null && CurrentOperation.State == ServerDragDropDataOperation.DragDropState.Dragging)
            {
                //If the dragdrop gets sent back to the operation host, cancel the operation.
                if(newActiveClient == CurrentOperation.Host)
                {
                    ISLogger.Write("Dragdrop returned to sender, cancelling dragdrop operation");
                    SetState(ServerDragDropDataOperation.DragDropState.Cancelled);
                    return;
                }

                CurrentOperation.TargetClient = newActiveClient;

                if (!oldActiveClient.IsLocalhost && newActiveClient.IsLocalhost)
                    DoLocalDragDrop();
                else if (oldActiveClient.IsLocalhost && !newActiveClient.IsLocalhost && newActiveClient.IsConnected)
                    newActiveClient.SendDragDropData(CurrentOperation.Data.ToBytes(), CurrentOperation.OperationGuid);

                if(!oldActiveClient.IsLocalhost && oldActiveClient.IsConnected)
                {
                    oldActiveClient.SendCancelDragDrop();
                }

            }else if(oldActiveClient.IsLocalhost && !newActiveClient.IsLocalhost)
            {
                ISLogger.Write("Checking for drop!");
                ddManager.CheckForDrop();
            }
        }
    }
}
