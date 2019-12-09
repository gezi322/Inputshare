using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InputshareLib.Clipboard;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Net;
using InputshareLib.PlatformModules.Clipboard;

namespace InputshareLib.Server
{
    internal class GlobalClipboardController
    {
        public event EventHandler<ServerDataOperation> ClipboardDataChanged;
        internal bool GlobalClipboardEnabled { get; set; } = true;

        internal ServerDataOperation CurrentOperation { get; private set; }
        private ClipboardManagerBase cbManager;
        private ClientManager clientMan;

        internal GlobalClipboardController(ClipboardManagerBase clipManager, ClientManager clientManager)
        {
            cbManager = clipManager;
            clientMan = clientManager;

            cbManager.ClipboardContentChanged += OnLocalClipboardChange;
        }

        public void OnClientClipboardChange(object sender, NetworkSocket.ClipboardDataReceivedArgs args)
        {
            if (!GlobalClipboardEnabled)
                return;

            ISServerSocket client = sender as ISServerSocket;
            ClipboardDataBase cbData = ClipboardDataBase.FromBytes(args.RawData);

            //Create the operation with the GUID that the client sent. We can use this GUID to request an access
            //token if the data contains files
            SetOperation(new ServerDataOperation(cbData, client, args.OperationId));
        }

        private void OnLocalClipboardChange(object sender, ClipboardDataBase data)
        {
            if (!GlobalClipboardEnabled)
                return;

            //Create a new dataoperation, a guid will be created generated automatically
            ServerDataOperation operation = new ServerDataOperation(data, ISServerSocket.Localhost);
            SetOperation(operation);
        }

        private void SetOperation(ServerDataOperation operation)
        {
            try
            {
                CurrentOperation = operation;
                BroadcastOperation();
                ClipboardDataChanged?.Invoke(this, CurrentOperation);
                ISLogger.Write("Clipboard operation set! type = {0} host = {1}", CurrentOperation.Data.DataType, CurrentOperation.Host);
            }catch(Exception ex)
            {
                ISLogger.Write("GlobalClipboardController: Error setting global clipboard operation: " + ex.Message);
                ISLogger.Write(ex.StackTrace);
            }
        }

        /// <summary>
        /// Notify all connected clients of a clipboard data change, and sets the local clipboard data
        /// to the data stored in CurrentOperation
        /// </summary>
        /// <returns></returns>
        private void BroadcastOperation()
        {
            if (!CurrentOperation.Host.IsLocalhost)
            {
                if (CurrentOperation.Data is ClipboardVirtualFileData cbFiles)
                {
                    cbFiles.RequestPartMethod = CurrentOperation.Host.RequestReadStreamAsync;
                    cbFiles.RequestTokenMethod = CurrentOperation.Host.RequestFileTokenAsync;
                }

                cbManager.SetClipboardData(CurrentOperation.Data);
            }

            byte[] data = CurrentOperation.Data.ToBytes();
            //Send the data to any client except the host client and localhost
            foreach (var client in clientMan.AllClients.Where(c => !c.IsLocalhost && c.IsConnected && c != CurrentOperation.Host))
            {
                client.SendClipboardData(data, CurrentOperation.OperationGuid);
            }

            data = null;
        }
    }
}
