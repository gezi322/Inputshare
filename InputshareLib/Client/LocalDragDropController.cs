using InputshareLib.Clipboard;
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
        internal ClientDataOperation CurrentOperation { get; private set; }


        private readonly DragDropManagerBase ddManager;
        public ISClientSocket Server { get; set; }
        private Dictionary<Guid, ClientDataOperation> previousOperations = new Dictionary<Guid, ClientDataOperation>();

        internal LocalDragDropController(DragDropManagerBase dragDropManager)
        {
            ddManager = dragDropManager;
        }

        internal void Socket_DragDropReceived(object sender, NetworkSocket.DragDropDataReceivedArgs args)
        {
            BeginReceivedOperation(args);

        }

        internal void Socket_DragDropCancelled(object sender, EventArgs _)
        {
            ddManager.CancelDrop();
        }

        internal void Socket_CancelAnyDragDrop(object sender, EventArgs _)
        {
            ddManager.CancelDrop();
        }

        internal void Local_DataDropped(object sender, ClipboardDataBase cbData)
        {
            if (!Server.IsConnected)
                return;

            if (CurrentOperation != null && !previousOperations.ContainsKey(CurrentOperation.OperationGuid))
            {
                previousOperations.Add(CurrentOperation.OperationGuid, CurrentOperation);
            }

            if (Server.IsConnected)
            {
                Guid opId = Guid.NewGuid();
                CurrentOperation = new ClientDataOperation(cbData, opId);
                ISLogger.Write("LocalDragDropController: Started dragdrop operation " + opId);
                Server.SendDragDropData(cbData.ToBytes(), opId);
            }
        }

        internal void Local_DragDropSuccess(object sender, EventArgs _)
        {
            if (!Server.IsConnected)
                return;

            Server?.NotifyDragDropSuccess(true);
        }

        internal void Local_DragDropCancelled(object sender, EventArgs _)
        {
            if (!Server.IsConnected)
                return;

            Server?.NotifyDragDropSuccess(false);
        }

        private void BeginReceivedOperation(NetworkSocket.DragDropDataReceivedArgs args)
        {
            //Check if the received operation has previously been received
            if (CurrentOperation?.OperationGuid == args.OperationId)
            {
                ddManager.DoDragDrop(CurrentOperation.Data);
                return;
            }

            if (CurrentOperation != null && !previousOperations.ContainsKey(CurrentOperation.OperationGuid))
                previousOperations.Add(CurrentOperation.OperationGuid, CurrentOperation);


            ClipboardDataBase cbData = ClipboardDataBase.FromBytes(args.RawData);
            cbData.OperationId = args.OperationId;

            //We need to setup the virtual files if this is a file drop
            if (cbData is ClipboardVirtualFileData cbFiles)
            {
                cbFiles.RequestPartMethod = Server.RequestReadStreamAsync;
                cbFiles.RequestTokenMethod = Server.RequestFileTokenAsync;
            }

            CurrentOperation = new ClientDataOperation(cbData, args.OperationId);
            ddManager.DoDragDrop(cbData);
        }
    }
}
