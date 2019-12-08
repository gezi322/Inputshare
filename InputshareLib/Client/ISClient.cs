using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using InputshareLib.Clipboard;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Displays;
using InputshareLib.FileController;
using InputshareLib.Input;
using InputshareLib.Net;
using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Displays;
using InputshareLib.PlatformModules.DragDrop;
using InputshareLib.PlatformModules.Output;

namespace InputshareLib.Client
{
    public class ISClient
    {
        public bool Running { get; private set; }

        /// <summary>
        /// Fires when alt+ctrl+delete is received
        /// </summary>
        public event EventHandler SasRequested;

        public event EventHandler<IPEndPoint> Connected;
        public event EventHandler<string> ConnectionFailed;
        public event EventHandler<string> ConnectionError;
        public event EventHandler Disconnected;
        public event EventHandler<bool> ActiveClientChanged;

        public bool ActiveClient { get; private set; }
        public string ClientName { get; set; } = Environment.MachineName;
        public Guid ClientId { get; private set; } = Guid.NewGuid();
        public bool AutoReconnect { get => server.AutoReconnect; set => server.AutoReconnect = value; }
        public IPEndPoint ServerAddress { get => server.ServerAddress; }

        public bool IsConnected { get => server.IsConnected; }

        //Modules
        private OutputManagerBase outMan;
        private ClipboardManagerBase clipboardMan;
        private DragDropManagerBase dragDropMan;
        private DisplayManagerBase displayMan;

        private FileAccessController fileController;
        private LocalDragDropController ddController;
        private LocalClipboardController cbController;
        private ClientEdges assignedEdges;

        private ISClientSocket server;
        private StartOptions startArgs;

        /// <summary>
        /// Starts the client with specified modules
        /// </summary>
        /// <param name="args"></param>
        /// <param name="dependencies"></param>
        public void Start(StartOptions args, ISClientDependencies dependencies)
        {
            if (Running)
                throw new InvalidOperationException("Client already running");

            try
            {
                startArgs = args;
                ISLogger.Write("Starting inputshare client...");
                ISLogger.Write("Using args:");
                startArgs.PrintArgs();

                InitDependencies(dependencies);
                InitSocket();
                fileController = new FileAccessController();
                cbController = new LocalClipboardController(clipboardMan, server);
                ddController = new LocalDragDropController(dragDropMan, server);
                AssignSocketEvents();
                AssignModuleEvents();
                StartModules();
                Running = true;
            }catch(Exception ex)
            {
                ISLogger.Write("An error occurred while starting client: " + ex.Message);
                ISLogger.Write(ex.StackTrace);
                Stop();
                throw ex;
            }
        }

        /// <summary>
        /// Stops the client
        /// </summary>
        public void Stop()
        {
            if (!Running)
                throw new InvalidOperationException("Client not running");

            try
            {
                ISLogger.Write("Stopping inputshare client...");

                if (IsConnected)
                    server.Disconnect(true);

                server.Dispose();
                fileController.DeleteAllTokens();
                StopModules();
            }catch(Exception ex)
            {
                ISLogger.Write("an error occurred while stopping client: " + ex.Message);
                ISLogger.Write(ex.StackTrace);
                throw ex;
            }
            finally
            {
                Running = false;
            }
            
            
        }

        /// <summary>
        /// Attempts to connect to an inputshare server
        /// </summary>
        /// <param name="address">Inputshare server address</param>
        public void Connect(IPEndPoint address)
        {
            if (IsConnected)
                throw new InvalidOperationException("Client already connected");

            if (!Running)
                throw new InvalidOperationException("Client not running");

            server.Connect(address, new ISClientSocket.ConnectionInfo(ClientName, ClientId, displayMan.CurrentConfig.ToBytes()));
        }

        public void Disconnect()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Client already disconnected");

            if (!Running)
                throw new InvalidOperationException("Client not running");

            server.Disconnect(true);
            Disconnected?.Invoke(this, null);
        }

        private void InitSocket()
        {
            server = new ISClientSocket(!startArgs.HasArg(StartArguments.NoUdp));
        }

        private void InitDependencies(ISClientDependencies deps)
        {
            if (startArgs.HasArg(StartArguments.NoDragDrop))
                dragDropMan = new NullDragDropManager();
            else
                dragDropMan = deps.dragDropManager;

            if (startArgs.HasArg(StartArguments.NoClipboard))
                clipboardMan = new NullClipboardManager();
            else
                clipboardMan = deps.clipboardManager; 

            displayMan = deps.displayManager;
            outMan = deps.outputManager;

            ISLogger.Write("Client dependencies:");
            ISLogger.Write(clipboardMan.GetType().Name);
            ISLogger.Write(displayMan.GetType().Name);
            ISLogger.Write(dragDropMan.GetType().Name);
            ISLogger.Write(outMan.GetType().Name);
        }

        private void StartModules()
        {
            if (!clipboardMan.Running)
                clipboardMan.Start();
            if (!dragDropMan.Running)
                dragDropMan.Start();
            if (!outMan.Running)
                outMan.Start();
            if (!displayMan.Running)
                displayMan.Start();
        }

        private void StopModules()
        {
            if (clipboardMan.Running)
                clipboardMan.Stop();
            if (dragDropMan.Running)
                dragDropMan.Stop();
            if (outMan.Running)
                outMan.Stop();
            if (displayMan.Running)
                displayMan.Stop();
        }

        private void AssignSocketEvents()
        {
            server.Connected += (object o, EventArgs e) => { Connected?.Invoke(this, server.ServerAddress); };
            server.ConnectionError += ConnectionError;
            server.ConnectionFailed += ConnectionFailed;
            server.InputDataReceived += Server_InputDataReceived;
            server.ActiveClientChanged += (object o, bool active) => { ActiveClient = active; outMan.ResetKeyStates(); ActiveClientChanged?.Invoke(this, active); };
            server.EdgesChanged += Server_EdgesChanged;
            server.RequestedStreamRead += fileController.Client_RequestedStreamRead;
            server.RequestedFileToken += Server_FileTokenRequested;
        }

        private void AssignModuleEvents()
        {
            displayMan.DisplayConfigChanged += (object o, DisplayConfig conf) =>
            {
                if (server.IsConnected)
                    server.SendDisplayConfig(conf.ToBytes());
            };

            displayMan.EdgeHit += DisplayMan_EdgeHit;
        }

        private void DisplayMan_EdgeHit(object sender, Edge edge)
        {
            if (server != null && server.IsConnected && ActiveClient)
            {
                server.SendEdgeHit(edge);

                if (dragDropMan.LeftMouseState)
                {
                    switch (edge)
                    {
                        case Edge.Bottom: if (assignedEdges.Bottom) dragDropMan.CheckForDrop(); break;
                        case Edge.Left: if (assignedEdges.Left) dragDropMan.CheckForDrop(); break;
                        case Edge.Right: if (assignedEdges.Right) dragDropMan.CheckForDrop(); break;
                        case Edge.Top: if (assignedEdges.Top) dragDropMan.CheckForDrop(); break;
                    }
                }
            }
        }

        private void Server_FileTokenRequested(object sender, NetworkSocket.FileTokenRequestArgs args)
        {
            ISLogger.Write("Server requested token for operation");

            ClientDataOperation op = null;
            if (cbController.CurrentOperation != null && cbController.CurrentOperation.OperationGuid == args.DataOperationId && cbController.CurrentOperation.Data.DataType == ClipboardDataType.File)
                op = cbController.CurrentOperation;
            else if (ddController.CurrentOperation != null && ddController.CurrentOperation.OperationGuid == args.DataOperationId && ddController.CurrentOperation.Data.DataType == ClipboardDataType.File)
                op = ddController.CurrentOperation;


            if (op != null)
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

        private void Server_EdgesChanged(object sender, ISClientSocket.BoundEdges e)
        {
            assignedEdges.Bottom = e.Bottom;
            assignedEdges.Left = e.Left;
            assignedEdges.Right = e.Right;
            assignedEdges.Top = e.Top;
        }

        private void Server_InputDataReceived(object sender, byte[] data)
        {
            ISInputData input = new ISInputData(data);

            if (input.Code == ISInputCode.IS_SENDSAS)
            {
                SasRequested?.Invoke(this, new EventArgs());
                return;
            }

            outMan.Send(new Input.ISInputData(data));
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
