﻿using InputshareLib.Clipboard;
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
                if (server == null)
                    return false;
                else
                    return server.IsConnected;
            }
        }

        public bool ActiveClient { get; private set; }
        public IPEndPoint ServerAddress { get => lastConnectedAddress; }

        public event EventHandler<bool> ActiveClientChanged;
        public event EventHandler<IPEndPoint> Connected;
        public event EventHandler<string> ConnectionFailed;
        public event EventHandler<string> ConnectionError;
        public event EventHandler Disconnected;

        private ClientEdges edges;
        private readonly OutputManagerBase outMan;
        private readonly ClipboardManagerBase clipboardMan;
        private readonly ISClientSocket server;

        private readonly DisplayManagerBase displayMan;
        private readonly DragDropManagerBase dragDropMan;

        public string ClientName { get; private set; } = Environment.MachineName;
        public Guid ClientId { get; private set; } = Guid.NewGuid();
        private IPEndPoint lastConnectedAddress;

        /// <summary>
        /// If true, the client will automatically keep trying to reconnect to
        /// the last connected host
        /// </summary>
        public bool AutoReconnect { get => server.AutoReconnect; set => server.AutoReconnect = value; }

        private FileAccessController fileController = new FileAccessController();
        private LocalDragDropController ddController;
        private LocalClipboardController cbController;

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

            server = new ISClientSocket(!args.HasArg(StartArguments.NoUdp));

            CreateSocketEvents();
            Init();
            ddController = new LocalDragDropController(dragDropMan, server);
            cbController = new LocalClipboardController(clipboardMan, server);


            dragDropMan.DragDropSuccess += ddController.Local_DragDropSuccess;
            dragDropMan.DragDropCancelled += ddController.Local_DragDropCancelled;
            dragDropMan.DataDropped += ddController.Local_DataDropped;
            server.CancelAnyDragDrop += ddController.Socket_CancelAnyDragDrop;
            server.DragDropDataReceived += ddController.Socket_DragDropReceived;
            server.DragDropCancelled += ddController.Socket_DragDropCancelled;

            server.ClipboardDataReceived += cbController.OnClipboardDataReceived;

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

            server?.Close();
        }

        public void Disconnect()
        {
            if (!IsConnected)
                throw new InvalidOperationException("not connected");

            server.Disconnect(true);
            Disconnected?.Invoke(this, null);
        }

        private void Init()
        {
            displayMan.Start();
            displayMan.DisplayConfigChanged += OnLocalDisplayConfigChange;
            displayMan.UpdateConfigManual();
            displayMan.EdgeHit += OnLocalEdgeHit;
            clipboardMan.Start();
            dragDropMan.Start();
        }
        private void OnLocalEdgeHit(object sender, Edge edge)
        {
            if (server != null && server.IsConnected && ActiveClient)
            {
                server.SendEdgeHit(edge);

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
            if (server != null && server.IsConnected)
            {
                server.SendDisplayConfig(config.ToBytes());
            }
        }

        public void Connect(string address, int port)
        {
            if (!IPAddress.TryParse(address, out IPAddress addr))
                throw new ArgumentException("Invalid address");

            if (port < 0 || port > 65535)
                throw new ArgumentException("Invalid port");


            if (server.AttemptingConnection)
            {
                ISLogger.Write("Already attempting connection... ingoring request");
                return;
            }

            lastConnectedAddress = new IPEndPoint(addr, port);
            server.Connect(address, port, new ISClientSocket.ConnectionInfo(ClientName, ClientId, displayMan.CurrentConfig.ToBytes()));
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
            server.Connected += OnConnected;
            server.ConnectionError += OnConnectionError;
            server.ConnectionFailed += OnConnectionFailed;
            server.InputDataReceived += OnInputReceived;
            server.ActiveClientChanged += OnActiveClientChange;
            server.EdgesChanged += Socket_EdgesChanged;
            server.RequestedFileToken += Socket_FileTokenRequested;
            server.RequestedStreamRead += Socket_RequestStreamRead;
            server.RequestedCloseStream += Socket_RequestedCloseStream;
        }

        private void Socket_RequestedCloseStream(object sender, NetworkSocket.RequestCloseStreamArgs e)
        {
            fileController.CloseStream(e.Token, e.File);
        }


        private void Socket_RequestStreamRead(object sender, NetworkSocket.RequestStreamReadArgs args)
        {
            if (!fileController.DoesTokenExist(args.Token))
            {
                server.SendFileErrorResponse(args.NetworkMessageId, "Failed to read file: Token not found " + args.Token);
                return;
            }

            try
            {
                byte[] data = new byte[args.ReadLen];
                int readLen = fileController.ReadStream(args.Token, args.File, data, 0, args.ReadLen);

                //resize the buffer so we don't send a buffer that ends with empty data.
                if (data.Length != readLen)
                {
                    byte[] resizedBuffer = new byte[readLen];
                    Buffer.BlockCopy(data, 0, resizedBuffer, 0, readLen);
                    data = resizedBuffer;
                }
                
                server.SendReadRequestResponse(args.NetworkMessageId, data);

            }
            catch (Exception ex)
            {
                server.SendFileErrorResponse(args.NetworkMessageId, ex.Message);
            }
        }

        private void Socket_FileTokenRequested(object sender, NetworkSocket.FileTokenRequestArgs args)
        {
            ISLogger.Write("Server requested token for operation");

            ClientDataOperation op = null;
            if (cbController.CurrentOperation != null && cbController.CurrentOperation.OperationGuid == args.DataOperationId && cbController.CurrentOperation.Data.DataType == ClipboardDataType.File)
                op = cbController.CurrentOperation;
            else if (ddController.CurrentOperation != null && ddController.CurrentOperation.OperationGuid == args.DataOperationId && ddController.CurrentOperation.Data.DataType == ClipboardDataType.File)
                op = ddController.CurrentOperation;
                

            if(op != null)
            {
                Guid token = fileController.CreateTokenForOperation(op, 10000);
                op.RemoteFileAccessTokens.Add(token);
                ISLogger.Write("Sending access token " + token);
                server.SendTokenRequestReponse(args.NetworkMessageId, token);
            }
            else
            {
                ISLogger.Write("Failed to send access token: Operation not found");
                server.SendFileErrorResponse(args.NetworkMessageId, "Failed to create token: Operation not found");
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
            Connected?.Invoke(this, server.ServerAddress);
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
