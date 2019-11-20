using InputshareLib.Clipboard;
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
        private ISClientSocket socket;
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



        private ClientDataOperation currentClipboardOperation;

        private FileAccessController fileController = new FileAccessController();
        private LocalDragDropController ddController;

        private StartOptions startArgs;

        public ISClient(ISClientDependencies dependencies, StartOptions args)
        {
            startArgs = args;

            displayMan = dependencies.displayManager;
            outMan = dependencies.outputManager;

            if (args.HasArg(StartArguments.Verbose))
                ISLogger.EnableConsole = true;

            if (args.HasArg(StartArguments.NoClipboard))
                clipboardMan = new NullClipboardManager();
            else
                clipboardMan = dependencies.clipboardManager;

            if (args.HasArg(StartArguments.NoDragDrop))
                dragDropMan = new NullDragDropManager();
            else
                dragDropMan = dependencies.dragDropManager;

            socket = new ISClientSocket(!args.HasArg(StartArguments.NoUdp));

            Init();
            CreateSocketEvents();

            if (args.HasArg(StartArguments.Connect))
                Connect(args.SpecifiedServer.Address.ToString(), args.SpecifiedServer.Port);

            AutoReconnect = startArgs.HasArg(StartArguments.AutoReconnect);

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
                //create GUID and file tokens
                Guid operationId = Guid.NewGuid();

                currentClipboardOperation = new ClientDataOperation(data, operationId);
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
        }

        private void Socket_RequestedCloseStream(object sender, NetworkSocket.RequestCloseStreamArgs e)
        {
            fileController.CloseStream(e.Token, e.File);
        }


        private void Socket_RequestStreamRead(object sender, NetworkSocket.RequestStreamReadArgs args)
        {
            if (!fileController.DoesTokenExist(args.Token))
            {
                socket.SendFileErrorResponse(args.NetworkMessageId, "Failed to read file: Token not found " + args.Token);
                return;
            }

            try
            {
                byte[] data = new byte[args.ReadLen];
                int readLen = fileController.ReadStream(args.Token, args.File, data, 0, args.ReadLen);
                //resize the buffer so we don't send a buffer that ends with empty data.
                byte[] resizedBuffer = new byte[readLen];
                Buffer.BlockCopy(data, 0, resizedBuffer, 0, readLen);
                socket.SendReadRequestResponse(args.NetworkMessageId, resizedBuffer);
            }
            catch (Exception ex)
            {
                socket.SendFileErrorResponse(args.NetworkMessageId, ex.Message);
            }
        }

        private void Socket_FileTokenRequested(object sender, NetworkSocket.FileTokenRequestArgs args)
        {
            ISLogger.Write("Server requested token for operation");

            ClientDataOperation op = null;
            if (currentClipboardOperation != null && currentClipboardOperation.OperationGuid == args.DataOperationId && currentClipboardOperation.Data.DataType == ClipboardDataType.File)
                op = currentClipboardOperation;
            else if (ddController.CurrentOperation != null && ddController.CurrentOperation.OperationGuid == args.DataOperationId && ddController.CurrentOperation.Data.DataType == ClipboardDataType.File)
                op = ddController.CurrentOperation;
                

            if(op != null)
            {
                Guid token = fileController.CreateTokenForOperation(op, 3000);
                op.RemoteFileAccessTokens.Add(token);
                ISLogger.Write("Sending access token " + token);
                socket.SendTokenRequestReponse(args.NetworkMessageId, token);
            }
            else
            {
                ISLogger.Write("Failed to send access token: Operation not found");
                socket.SendFileErrorResponse(args.NetworkMessageId, "Failed to create token: Operation not found");
                return;
            }
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

        private void OnClipboardDataReceived(object sender, NetworkSocket.ClipboardDataReceivedArgs args)
        {
            try
            {
                ISLogger.Write("Got clipboard operation " + args.OperationId);
                ClipboardDataBase cbData = ClipboardDataBase.FromBytes(args.RawData);

                if (cbData is ClipboardVirtualFileData cbFiles)
                {
                    cbFiles.RequestPartMethod = socket.RequestReadStreamAsync;
                    cbFiles.RequestTokenMethod = socket.RequestFileTokenAsync;
                    cbFiles.OperationId = args.OperationId;
                }

                currentClipboardOperation = new ClientDataOperation(cbData, args.OperationId);
                clipboardMan.SetClipboardData(cbData);
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to set clipboard data: " + ex.Message);
            }

            ClipboardDataCopied?.Invoke(this, null);
        }

        private struct ClientEdges
        {
            public bool Left;
            public bool Right;
            public bool Top;
            public bool Bottom;
        }

    }
}
