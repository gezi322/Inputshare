using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Displays;
using InputshareLib.Input;
using InputshareLib.Net;
using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Displays;
using InputshareLib.PlatformModules.DragDrop;
using InputshareLib.PlatformModules.Output;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib.Client
{
    public sealed class ISClient
    {

        /// <summary>
        /// Raised when the server sends SAS signal
        /// </summary>
        public event EventHandler SasRequested;

        public bool IsConnected
        {
            get
            {
                if (socket == null)
                    return false;
                else
                    return socket.IsConnected;
            }
        }

        public bool ActiveClient { get; private set; }
        public IPEndPoint ServerAddress { get => lastConnectedAddress; }

        public event EventHandler<bool> ActiveClientChanged;
        public event EventHandler<IPEndPoint> Connected;
        public event EventHandler<string> ConnectionFailed;
        public event EventHandler<string> ConnectionError;
        public event EventHandler Disconnected;
        public event EventHandler ClipboardDataCopied;

        private ClientEdges edges;
        private readonly OutputManagerBase outMan;
        private readonly ClipboardManagerBase clipboardMan;
        private ISClientSocket socket = new ISClientSocket();
        private readonly DisplayManagerBase displayMan;
        private readonly DragDropManagerBase dragDropMan;

        public string ClientName { get; private set; } = Environment.MachineName;
        public Guid ClientId { get; private set; } = Guid.NewGuid();
        private IPEndPoint lastConnectedAddress;

        /// <summary>
        /// If true, the client will automatically keep trying to reconnect to
        /// the last connected host
        /// </summary>
        public bool AutoReconnect { get => socket.AutoReconnect; set => socket.AutoReconnect = value; }



        private DataOperation currentClipboardOperation = new DataOperation();
        private DataOperation currentDragDropOperation = new DataOperation();

        private Dictionary<Guid, DataOperation> previousOperations = new Dictionary<Guid, DataOperation>();
        private FileAccessController fileController = new FileAccessController();
        private LocalDragDropController ddController;

        public ISClient(ISClientDependencies dependencies)
        {
            displayMan = dependencies.displayManager;
            outMan = dependencies.outputManager;
            clipboardMan = dependencies.clipboardManager;
            dragDropMan = dependencies.dragDropManager;
            ddController = new LocalDragDropController(fileController, dragDropMan);
            Init();
            CreateSocketEvents();
        }

        public void Stop()
        {
            if (displayMan.Running)
                displayMan.Stop();
            if (dragDropMan.Running)
                dragDropMan.Stop();
            if (clipboardMan.Running)
                clipboardMan.Stop();
            if (outMan.Running)
                outMan.Stop();

            socket?.Close();
        }

        public void Disconnect()
        {
            if (!IsConnected)
                throw new InvalidOperationException("not connected");

            socket.Disconnect(true);
            Disconnected?.Invoke(this, null);
        }

        private void Init()
        {
            displayMan.Start();
            displayMan.DisplayConfigChanged += OnLocalDisplayConfigChange;
            displayMan.UpdateConfigManual();
            displayMan.EdgeHit += OnLocalEdgeHit;
            clipboardMan.Start();
            clipboardMan.ClipboardContentChanged += OnLocalClipboardChange;
            dragDropMan.Start();
            dragDropMan.DragDropSuccess += ddController.Local_DragDropSuccess;
            dragDropMan.DragDropCancelled += ddController.Local_DragDropCancelled;
            dragDropMan.DragDropComplete += ddController.Local_DragDropComplete;
            dragDropMan.DataDropped += ddController.Local_DataDropped;
            dragDropMan.FileDataRequested += DragDropMan_FileDataRequested;
        }
        private async void DragDropMan_FileDataRequested(object sender, DragDropManagerBase.RequestFileDataArgs e)
        {
            try
            {
                byte[] data = await socket.RequestReadStreamAsync(e.Token, e.FileId, e.ReadLen);
                dragDropMan.WriteToFile(e.MessageId, data);
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to read external file stream: " + ex.Message);
                dragDropMan.WriteToFile(e.MessageId, new byte[0]);
            }
        }

        private void OnLocalClipboardChange(object sender, ClipboardDataBase data)
        {
            if (socket != null && socket.IsConnected)
            {

                if (data.DataType == ClipboardDataType.File)
                {
                    ISLogger.Write("File copying/pasting is currently disabled....");
                    return;
                }

                //create GUID and file tokens
                Guid operationId = Guid.NewGuid();

                if (currentDragDropOperation.OperationId != Guid.Empty)
                    previousOperations.Add(currentClipboardOperation.OperationId, currentClipboardOperation);

                currentClipboardOperation = new DataOperation(operationId, data);
                socket.SendClipboardData(data.ToBytes(), operationId);
                ISLogger.Write("Created clipboard operation " + operationId);
            }
        }

        private void OnLocalEdgeHit(object sender, Edge edge)
        {
            if (socket != null && socket.IsConnected && ActiveClient)
            {
                socket.SendEdgeHit(edge);

                if (dragDropMan.LeftMouseState)
                {
                    switch (edge)
                    {
                        case Edge.Bottom: if (edges.Bottom) dragDropMan.CheckForDrop(); break;
                        case Edge.Left: if (edges.Left) dragDropMan.CheckForDrop(); break;
                        case Edge.Right: if (edges.Right) dragDropMan.CheckForDrop(); break;
                        case Edge.Top: if (edges.Top) dragDropMan.CheckForDrop(); break;
                    }
                }

            }

        }

        private void OnLocalDisplayConfigChange(object sender, DisplayConfig config)
        {
            if (socket != null && socket.IsConnected)
            {
                socket.SendDisplayConfig(config.ToBytes());
            }
        }

        public void Connect(string address, int port)
        {
            if (!IPAddress.TryParse(address, out IPAddress addr))
                throw new ArgumentException("Invalid address");

            if (port < 0 || port > 65535)
                throw new ArgumentException("Invalid port");


            if (socket != null && socket.AttemptingConnection)
            {
                ISLogger.Write("Already attempting connection... ingoring request");
                return;
            }


            ddController.Server = socket; //TODO - bad design
            lastConnectedAddress = new IPEndPoint(addr, port);
            socket.Connect(address, port, new ISClientSocket.ConnectionInfo(ClientName, ClientId, displayMan.CurrentConfig.ToBytes()));
        }

        public void SetClientGuid(Guid newGuid)
        {
            ClientId = newGuid;
        }

        public void SetClientName(string name)
        {
            ClientName = name;
        }

        private void CreateSocketEvents()
        {
            socket.ClipboardDataReceived += OnClipboardDataReceived;
            socket.Connected += OnConnected;
            socket.ConnectionError += OnConnectionError;
            socket.ConnectionFailed += OnConnectionFailed;
            socket.InputDataReceived += OnInputReceived;
            socket.ActiveClientChanged += OnActiveClientChange;
            socket.EdgesChanged += Socket_EdgesChanged;
            socket.RequestedFileToken += Socket_FileTokenRequested;
            socket.RequestedStreamRead += Socket_RequestStreamRead;
            socket.RequestedCloseStream += Socket_RequestedCloseStream;
            socket.CancelAnyDragDrop += ddController.Socket_CancelAnyDragDrop;
            socket.DragDropDataReceived += ddController.Socket_DragDropReceived;
            socket.DragDropCancelled += ddController.Socket_DragDropCancelled;
            socket.DragDropOperationComplete += ddController.Socket_DragDropComplete;
        }

        private void Socket_RequestedCloseStream(object sender, NetworkSocket.RequestCloseStreamArgs e)
        {
            fileController.CloseStream(e.Token, e.File);
        }


        private void Socket_RequestStreamRead(object sender, NetworkSocket.RequestStreamReadArgs e)
        {
            try
            {
                byte[] data = new byte[e.ReadLen];
                int readLen = fileController.ReadStream(e.Token, e.File, data, 0, e.ReadLen);
                //resize the buffer so we don't send a buffer that ends with empty data.
                byte[] resizedBuffer = new byte[readLen];
                Buffer.BlockCopy(data, 0, resizedBuffer, 0, readLen);
                socket.SendReadRequestResponse(e.NetworkMessageId, resizedBuffer);
            }
            catch (FileAccessController.TokenNotFoundException)
            {
                socket.SendFileErrorResponse(e.NetworkMessageId, "Token not found");
            }
            catch (Exception ex)
            {
                ISLogger.Write("Responding with: Read error - " + ex.Message);
                socket.SendFileErrorResponse(e.NetworkMessageId, ex.Message);
            }
        }

        private void Socket_FileTokenRequested(object sender, NetworkSocket.FileTokenRequestArgs args)
        {
            if (args.FileGroupId == Guid.Empty)
            {
                ISLogger.Write("Debug: server requested access to a blank file group ID");
                return;
            }
            ISLogger.Write("Server requested token");
            int timeout = 0;
            DataOperation operation;
            if (args.FileGroupId == currentClipboardOperation.OperationId)
            {
                operation = currentClipboardOperation;

                if (operation.Data.DataType == ClipboardDataType.File)
                {
                    ISLogger.Write("Responding to token request: Copy/Pasting files not yet implemented");
                    socket.SendFileErrorResponse(args.NetworkMessageId, "Copy/Pasting files not yet implemented");
                    return;
                }
            }

            else if (args.FileGroupId == ddController.CurrentOperation?.OperationId)
            {
                operation = new DataOperation(ddController.CurrentOperation.OperationId, ddController.CurrentOperation.Data);
                timeout = 10000;
            }
            else
            {
                socket.SendFileErrorResponse(args.NetworkMessageId, "Token not found");
                //todo - return error
                ISLogger.Write("Server requested token for invalid operation");
                return;
            }

            try
            {
                Guid token = CreateTokensForOperation(operation, timeout);

                //we need to keep track of which tokens are assoicated with which transfer

                if (operation.OperationId == ddController.CurrentOperation?.OperationId)
                    ddController.CurrentOperation.AssociatedAccessToken = token;
                else
                    currentClipboardOperation.AssociatedAccessTokens.Add(token);

                ISLogger.Write("added associated access token " + token);
                socket.SendTokenRequestReponse(args.NetworkMessageId, token);
            }
            catch (Exception ex)
            {
                socket.SendFileErrorResponse(args.NetworkMessageId, "Failed to create token: " + ex.Message);
                ISLogger.Write("Failed to create access token for operation: " + ex.Message);
                return;
            }

        }

        private Guid CreateTokensForOperation(DataOperation operation, int timeout)
        {
            ClipboardVirtualFileData fd = operation.Data as ClipboardVirtualFileData;

            Guid[] ids = new Guid[fd.AllFiles.Count];
            string[] sources = new string[fd.AllFiles.Count];
            for (int i = 0; i < fd.AllFiles.Count; i++)
            {
                ids[i] = fd.AllFiles[i].FileRequestId;
                sources[i] = fd.AllFiles[i].FullPath;
            }
            return fileController.CreateFileReadTokenForGroup(new FileAccessController.FileAccessInfo(ids, sources), timeout);
        }


        private void Socket_EdgesChanged(object sender, ISClientSocket.BoundEdges e)
        {
            edges.Bottom = e.Bottom;
            edges.Left = e.Left;
            edges.Right = e.Right;
            edges.Top = e.Top;
        }


        private void OnActiveClientChange(object sender, bool active)
        {
            ActiveClient = active;
            outMan.ResetKeyStates();
            ActiveClientChanged?.Invoke(this, ActiveClient);
        }

        private void OnInputReceived(object sender, byte[] data)
        {
            ISInputData input = new ISInputData(data);

            if (input.Code == ISInputCode.IS_SENDSAS)
            {
                SasRequested?.Invoke(this, new EventArgs());
                return;
            }

            outMan.Send(new Input.ISInputData(data));
        }

        private void OnConnectionFailed(object sender, string reason)
        {
            ISLogger.Write("Connection failed: " + reason);
            ConnectionFailed?.Invoke(this, reason);
        }

        private void OnConnectionError(object sender, string reason)
        {
            ISLogger.Write("Connection error: " + reason);
            ConnectionError?.Invoke(this, reason);
        }

        private void OnConnected(object sender, EventArgs e)
        {
            ISLogger.Write("Connected");
            Connected?.Invoke(this, socket.ServerAddress);
        }

        private async void OnClipboardDataReceived(object sender, NetworkSocket.ClipboardDataReceivedArgs args)
        {
            try
            {
                ISLogger.Write("Got clipboard operation " + args.OperationId);
                ClipboardDataBase cbData = ClipboardDataBase.FromBytes(args.RawData);


                if (cbData is ClipboardVirtualFileData cbFiles)
                {
                    Guid id = await socket.RequestFileTokenAsync(args.OperationId);

                    if (id == Guid.Empty)
                        return;

                    for (int i = 0; i < cbFiles.AllFiles.Count; i++)
                    {
                        cbFiles.AllFiles[i].RemoteAccessToken = id;
                        cbFiles.AllFiles[i].ReadDelegate = File_RequestDataAsync;
                        cbFiles.AllFiles[i].ReadComplete += VirtualFile_ReadComplete;
                        cbFiles.AllFiles[i].FileOperationId = args.OperationId;
                    }
                }

                currentClipboardOperation = new DataOperation(args.OperationId, cbData);
                clipboardMan.SetClipboardData(cbData);
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to set clipboard data: " + ex.Message);
            }

            ClipboardDataCopied?.Invoke(this, null);
        }

        private void VirtualFile_ReadComplete(object sender, EventArgs e)
        {

        }

        private async Task<byte[]> File_RequestDataAsync(Guid token, Guid operationId, Guid fileId, int readLen)
        {
            return await socket.RequestReadStreamAsync(token, fileId, readLen);
        }

        private struct ClientEdges
        {
            public bool Left;
            public bool Right;
            public bool Top;
            public bool Bottom;
        }

        struct DataOperation
        {
            public DataOperation(Guid operationId, ClipboardDataBase data)
            {
                OperationId = operationId;
                Data = data;
                AssociatedAccessTokens = new List<Guid>();
                Completed = false;
            }
            public Guid OperationId { get; }
            public ClipboardDataBase Data { get; }

            public bool Completed { get; set; }

            public List<Guid> AssociatedAccessTokens { get; set; }
        }
    }
}
