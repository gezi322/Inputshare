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

        internal bool DragDropEnabled { get; set; } = true;

        private readonly DragDropManagerBase ddManager;
        private readonly ISClientSocket server;

        public LocalDragDropController(DragDropManagerBase dragDropManager, ISClientSocket serverConnection)
        {
            ddManager = dragDropManager;
            server = serverConnection;
            ddManager.DataDropped += Local_DataDropped;
            ddManager.DragDropCancelled += Local_DragDropCancelled;
            ddManager.DragDropSuccess += Local_DragDropSuccess;
            server.DragDropDataReceived += Socket_DragDropReceived;
            server.CancelAnyDragDrop += Socket_CancelAnyDragDrop;
        }

        internal void Socket_DragDropReceived(object sender, NetworkSocket.DragDropDataReceivedArgs args)
        {
            BeginReceivedOperation(args);
        }

        internal void Socket_CancelAnyDragDrop(object sender, EventArgs _)
        {
            ddManager.CancelDrop();
        }

        internal void Local_DataDropped(object sender, ClipboardDataBase cbData)
        {
            if (!server.IsConnected)
                return;

            if (server.IsConnected)
            {
                Guid opId = Guid.NewGuid();
                CurrentOperation = new ClientDataOperation(cbData, opId);
                ISLogger.Write("LocalDragDropController: Started dragdrop operation " + opId);
                server.SendDragDropData(cbData.ToBytes(), opId);
            }
        }

        internal void Local_DragDropSuccess(object sender, EventArgs _)
        {
            if (!server.IsConnected)
                return;
            server?.NotifyDragDropSuccess(true);
        }

        internal void Local_DragDropCancelled(object sender, EventArgs _)
        {
            if (!server.IsConnected)
                return;

            server?.NotifyDragDropSuccess(false);
        }

        private void BeginReceivedOperation(NetworkSocket.DragDropDataReceivedArgs args)
        {
            if (!DragDropEnabled)
            {
                server.NotifyDragDropSuccess(false);
                return;
            }

            //Check if the received operation has previously been received
            if (CurrentOperation?.OperationGuid == args.OperationId)
            {
                ddManager.DoDragDrop(CurrentOperation.Data);
                return;
            }

            ClipboardDataBase cbData = ClipboardDataBase.FromBytes(args.RawData);
            cbData.OperationId = args.OperationId;

            //We need to setup the virtual files if this is a file drop
            if (cbData is ClipboardVirtualFileData cbFiles)
            {
                cbFiles.RequestPartMethod = server.RequestReadStreamAsync;
                cbFiles.RequestTokenMethod = server.RequestFileTokenAsync;

                ISLogger.Write("Copied files");

                foreach (var file in cbFiles.AllFiles)
                    ISLogger.Write(file.FullPath);
            }

            CurrentOperation = new ClientDataOperation(cbData, args.OperationId);
            ddManager.DoDragDrop(cbData);
        }
    }
}
