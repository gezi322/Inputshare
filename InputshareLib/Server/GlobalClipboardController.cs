using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Net;
using InputshareLib.PlatformModules.Clipboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InputshareLib.Server
{
    internal class GlobalClipboardController
    {
        public event EventHandler<ClipboardOperation> GlobalCLipboardChanged;

        private ClientManager clientMan;
        private FileAccessController fileController;
        private ClipboardManagerBase cbManager;
        private Dictionary<Guid, ClipboardOperation> previousOperationDictionary = new Dictionary<Guid, ClipboardOperation>();
        public ClipboardOperation currentOperation { get; private set; }

        public GlobalClipboardController(ClientManager clientManager, FileAccessController faController, ClipboardManagerBase cbMan)
        {
            cbManager = cbMan;
            fileController = faController;
            clientMan = clientManager;

            cbMan.Start();

            cbManager.ClipboardContentChanged += OnLocalClipboardDataCopied;
        }

        public void OnLocalClipboardDataCopied(object sender, ClipboardDataBase cbData)
        {
            //Generate a new GUID for this clipboard operation
            Guid opId = Guid.NewGuid();
            SetGlobalClipboard(cbData, ISServerSocket.Localhost, opId);
        }

        public void OnClientClipboardDataReceived(object sender, NetworkSocket.ClipboardDataReceivedArgs args)
        {
            ISServerSocket client = sender as ISServerSocket;
            ClipboardDataBase cbData = ClipboardDataBase.FromBytes(args.RawData);
            SetGlobalClipboard(cbData, client, args.OperationId);
        }

        private void SetGlobalClipboard(ClipboardDataBase data, ISServerSocket host, Guid operationId)
        {
            try
            {
                ISLogger.Write("New clipboard operation. Type {0}, Host {1}, ID {2}", data.DataType, host.ClientName, operationId);

                if (data.DataType == ClipboardDataType.File)
                {
                    SetClipboardFiles((data as ClipboardVirtualFileData), host, operationId);
                }
                else
                {
                    SetClipboardTextOrImage(data, host, operationId);
                }

                GlobalCLipboardChanged?.Invoke(this, currentOperation);
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to set global clipboard data: " + ex.Message);
            }

        }

        private async void SetClipboardFiles(ClipboardVirtualFileData cbFiles, ISServerSocket host, Guid operationId)
        {
            if (currentOperation != null && currentOperation.DataType == ClipboardDataType.File)
                previousOperationDictionary.Add(currentOperation.OperationId, currentOperation);

            if (currentOperation != null && currentOperation.LocalhostAccessToken != Guid.Empty)
                fileController.DeleteToken(currentOperation.LocalhostAccessToken);

            currentOperation = new ClipboardOperation(operationId, cbFiles.DataType, cbFiles, host);
            if (host == ISServerSocket.Localhost)
            {
                //TODO - We can't use the same access token for clipboard operations, as more than one client can
                //be reading from the stream at the same time which will cause corruption. 
                //We need to create a new access token for each client that pastes the files to create multiple stream instances


            }
            else
            {
                //Assign virtual file events, so if localhosts pastes the files then the shell can read data from the host.
                foreach (var file in cbFiles.AllFiles)
                {
                    file.ReadComplete += File_ReadComplete;
                    file.ReadDelegate = File_RequestDataAsync;
                }

                //Create a token so that localhost can access files

                try
                {
                    currentOperation.LocalhostAccessToken = await currentOperation.Host.RequestFileTokenAsync(operationId);
                }
                catch (Exception ex)
                {
                    ISLogger.Write("GlobalClipboardController: Failed to get access token for clipboard content: " + ex.Message);
                    return;
                }
            }

            BroadcastCurrentOperation();
        }

        public async Task<Guid> GenerateAccessToken(Guid operationId)
        {
            ClipboardOperation op = GetOperationFromId(operationId);

            if (op == null)
            {
                ISLogger.Write("GlobalClipboardController: Error generating access token: operation not found");
                return Guid.Empty;
            }

            Guid id;

            if (op.Host.IsLocalhost)
            {
                id = GenerateLocalAccessTokenForOperation(op);
            }
            else
            {
                try
                {
                    id = await op.Host.RequestFileTokenAsync(op.OperationId);
                }
                catch (Exception ex)
                {
                    ISLogger.Write("GlobalClipboardController: Failed to get access token for operation: " + ex.Message);
                    return Guid.Empty;
                }
            }

            op.HostFileAccessTokens.Add(id);
            return id;
        }


        public ClipboardOperation GetOperationFromId(Guid operationId)
        {
            if (currentOperation?.OperationId == operationId)
                return currentOperation;

            foreach (var op in previousOperationDictionary)
                if (op.Value.OperationId == operationId)
                    return op.Value;

            return null;
        }

        private void File_ReadComplete(object sender, EventArgs e)
        {
            ClipboardVirtualFileData.FileAttributes file = sender as ClipboardVirtualFileData.FileAttributes;
            ISLogger.Write("Debug: Clipboard: {0} read complete", file.FileName);
        }

        private async Task<byte[]> File_RequestDataAsync(Guid token, Guid operationId, Guid fileId, int readLen)
        {
            return await currentOperation.Host.RequestReadStreamAsync(token, fileId, readLen);
        }

        private void SetClipboardTextOrImage(ClipboardDataBase cbData, ISServerSocket host, Guid operationId)
        {
            if (currentOperation != null && currentOperation.DataType == ClipboardDataType.File)
                previousOperationDictionary.Add(currentOperation.OperationId, currentOperation);

            currentOperation = new ClipboardOperation(operationId, cbData.DataType, cbData, host);
            BroadcastCurrentOperation();
        }

        private void BroadcastCurrentOperation()
        {

            //All clients that are not localhost or the clipboard data host
            foreach (var client in clientMan.AllClients.Where(i => i != currentOperation.Host && i != ISServerSocket.Localhost))
            {
                client.SendClipboardData(currentOperation.Data.ToBytes(), currentOperation.OperationId);
            }

            //only set local clipboard if localhost is not the data host
            if (!currentOperation.Host.IsLocalhost)
            {
                cbManager.SetClipboardData(currentOperation.Data);
            }

        }


        private Guid GenerateLocalAccessTokenForOperation(ClipboardOperation operation)
        {
            if (operation.DataType != ClipboardDataType.File)
            {
                throw new ArgumentException("DateType must be file");
            }

            ClipboardVirtualFileData file = operation.Data as ClipboardVirtualFileData;
            Guid[] fIds = new Guid[file.AllFiles.Count];
            string[] fSources = new string[file.AllFiles.Count];

            for (int i = 0; i < file.AllFiles.Count; i++)
            {
                fIds[i] = file.AllFiles[i].FileRequestId;
                fSources[i] = file.AllFiles[i].FullPath;
            }

            return fileController.CreateFileReadTokenForGroup(new FileAccessController.FileAccessInfo(fIds, fSources), 0);
        }

        internal class ClipboardOperation
        {
            public ClipboardOperation(Guid operationId, ClipboardDataType dataType, ClipboardDataBase data, ISServerSocket host)
            {
                OperationId = operationId;
                DataType = dataType;
                Data = data;
                Host = host;
            }

            public Guid OperationId { get; }
            public ClipboardDataType DataType { get; }
            public ClipboardDataBase Data { get; }
            public ISServerSocket Host { get; }
            public Guid LocalhostAccessToken { get; set; }
            public List<Guid> HostFileAccessTokens { get; } = new List<Guid>();
        }
    }
}
