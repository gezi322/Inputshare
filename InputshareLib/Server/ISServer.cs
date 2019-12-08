using InputshareLib.Clipboard;
using InputshareLib.Displays;
using InputshareLib.FileController;
using InputshareLib.Input;
using InputshareLib.Input.Hotkeys;
using InputshareLib.Input.Keys;
using InputshareLib.Net;
using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Displays;
using InputshareLib.PlatformModules.DragDrop;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;
using InputshareLib.Server.API;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
namespace InputshareLib.Server
{
    public sealed class ISServer
    {

        //API events
        public event EventHandler Started;
        public event EventHandler Stopped;
        public event EventHandler<ClientInfo> ClientConnected;
        public event EventHandler<ClientInfo> ClientDisconnected;
        public event EventHandler<ClientInfo> ClientDisplayConfigChanged;
        public event EventHandler<ClientInfo> InputClientSwitched;
        public event EventHandler<CurrentClipboardData> GlobalClipboardContentChanged;
        public event EventHandler<ClientInfo> ClientInfoUpdated;

        public bool Running { get; private set; }
        public bool LocalInput { get; private set; }
        public ClientInfo CurrentInputClient { get => GetCurrentInputClient(); }
        public IPEndPoint BoundAddress { get; private set; }


        //OS dependant classes/interfaces
        private readonly DisplayManagerBase displayMan;
        private readonly InputManagerBase inputMan;
        private readonly DragDropManagerBase dragDropMan;
        private readonly OutputManagerBase outMan;
        private readonly ClipboardManagerBase cbManager;

        private ISClientListener clientListener;
        private readonly ClientManager clientMan;

        private ISUdpServer udpHost;

        /// <summary>
        /// Timer used to prevent switching back and forth between clients insantly
        /// </summary>
        private readonly Stopwatch clientSwitchTimer = new Stopwatch();

        /// <summary>
        /// Client that is currently being controlled
        /// </summary>
        private ISServerSocket inputClient = ISServerSocket.Localhost;


        private GlobalDragDropController ddController;
        private GlobalClipboardController cbController;
        private FileAccessController fileController;

        private StartOptions startArgs;

        #region Start/Stop

        public ISServer(ISServerDependencies dependencies, StartOptions args)
        {
            startArgs = args;

            if (args.HasArg(StartArguments.Verbose))
                ISLogger.EnableConsole = true;

            displayMan = dependencies.DisplayManager;
            inputMan = dependencies.InputManager;
            outMan = dependencies.OutputManager;
            dragDropMan = dependencies.DragDropManager;
            cbManager = dependencies.ClipboardManager;

            fileController = new FileAccessController();
            clientMan = new ClientManager(12);
            cbController = new GlobalClipboardController(cbManager, clientMan);
            ddController = new GlobalDragDropController(dragDropMan, clientMan);

            AssignEvents();
        }

        public void Start(int port)
        {
            if (Running)
                throw new InvalidOperationException("Server already running");

            try
            {
                Running = true;

                clientSwitchTimer.Restart();
                ISLogger.Write("Server: Starting server...");

                StartClientListener(new IPEndPoint(IPAddress.Any, port));
                clientListener.ClientConnected += ClientListener_ClientConnected;

                if(!startArgs.HasArg(StartArguments.NoUdp))
                    InitUdp(port);

                StartDisplayManager();

                if (!inputMan.Running)
                    inputMan.Start();

                AssignInitialHotkeys();

                if (!startArgs.HasArg(StartArguments.NoDragDrop))
                    dragDropMan.Start();
                else
                    ddController.GlobalDragDropEnabled = false;

                if (!startArgs.HasArg(StartArguments.NoClipboard))
                    cbManager.Start();
                else
                    cbController.GlobalClipboardEnabled = false;

                clientMan.AddClient(ISServerSocket.Localhost);
                ISLogger.Write("Server: Inputshare server started");
                Started?.Invoke(this, null);
            }
            catch (Exception ex)
            {
                Stop();
                throw ex;
            }
        }

        private void InitUdp(int port)
        {
            try
            {
                udpHost = new ISUdpServer(clientMan, port);
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to bind UDP! " + ex.Message);
            }
        }

        private void StartClientListener(IPEndPoint bind)
        {
            clientListener = new ISClientListener(bind.Port, bind.Address);
            BoundAddress = clientListener.BoundAddress;

        }

        private void AssignInitialHotkeys()
        {
            HotkeyModifiers mods = HotkeyModifiers.Ctrl | HotkeyModifiers.Alt | HotkeyModifiers.Shift;
            inputMan.AddUpdateFunctionHotkey(new FunctionHotkey(WindowsVirtualKey.Q, mods, Input.Hotkeys.Hotkeyfunction.StopServer));
            inputMan.AddUpdateClientHotkey(new ClientHotkey(WindowsVirtualKey.Z, HotkeyModifiers.Shift, Guid.Empty));
            inputMan.AddUpdateFunctionHotkey(new FunctionHotkey(WindowsVirtualKey.P, HotkeyModifiers.Alt | HotkeyModifiers.Ctrl, Hotkeyfunction.SendSas));
            ISServerSocket.Localhost.CurrentHotkey = new ClientHotkey(WindowsVirtualKey.Z, HotkeyModifiers.Shift, Guid.Empty);
        }

        private void StartDisplayManager()
        {
            displayMan.UpdateConfigManual();
            ISServerSocket.Localhost.DisplayConfiguration = displayMan.CurrentConfig;
            displayMan.Start();
        }

        private void AssignEvents()
        {
            displayMan.DisplayConfigChanged += DisplayMan_DisplayConfigChanged;
            displayMan.EdgeHit += DisplayMan_EdgeHit;
            inputMan.InputReceived += InputMan_InputReceived;
            inputMan.ClientHotkeyPressed += InputMan_ClientHotkeyPressed;
            inputMan.FunctionHotkeyPressed += InputMan_FunctionHotkeyPressed;
        }


        /// <summary>
        /// Stops the inputshare server
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the server is not running</exception>"
        public void Stop()
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            ISLogger.Write("Server: Stopping server...");


            try
            {
                foreach (var client in clientMan.AllClients)
                {
                    try
                    {
                        client.Close(NetworkSocket.CloseNotifyMode.ServerStopped);
                    }
                    catch (Exception) { }
                }
                clientMan.ClearClients();

                if (clientListener != null && clientListener.Listening)
                    clientListener.Stop();
                if (inputMan.Running)
                    inputMan.Stop();
                if (displayMan.Running)
                    displayMan.Stop();
                if (dragDropMan.Running)
                    dragDropMan.Stop();
                if (cbManager.Running)
                    cbManager.Stop();
                if (outMan.Running)
                    outMan.Stop();

                fileController.DeleteAllTokens();
                udpHost?.Dispose();

                ISLogger.Write("Server: server stopped.");

            }
            catch (Exception ex)
            {
                ISLogger.Write("An error occurred while stopping server: " + ex.Message);
                ISLogger.Write(ex.StackTrace);
            }
            finally
            {
                Running = false;
                Stopped?.Invoke(this, null);
            }

        }

        #endregion

        #region InputSwitching

        /// <summary>
        /// Switches the input client to the specified client and disables local input
        /// </summary>
        /// <param name="targetClient">The GUID of the target client</param>
        private void SwitchToClientInput(Guid targetClient)
        {
            //prevents a bug where if the mouse was on the very edge of the screen and not moving,
            //it would rapidly switch between clients
            if (clientSwitchTimer.ElapsedMilliseconds < 25)
                return;

            ISServerSocket client = clientMan.GetClientById(targetClient);

            if (client == null)
            {
                ISLogger.Write("Server: Failed to switch to client: client not found");
                return;
            }

            //We need to know the client that we are switching from to determine 
            //what to do if the user is dragging a file, specifically if we are switching 
            //from localhost
            ISServerSocket oldClient = inputClient;

            client.NotifyActiveClient(true);
            inputClient = client;

            //Disable local input
            inputMan.SetInputBlocked(true);

            clientSwitchTimer.Restart();

            //let the dragdrop controller determine if anything needs to be done or sent to the client
            ddController.HandleClientSwitch(oldClient, inputClient);
            InputClientSwitched?.Invoke(this, GenerateClientInfo(client));

        }

        /// <summary>
        /// Switches input back to localhost, and enables local input.
        /// </summary>
        private void SwitchToLocalInput()
        {
            if (clientSwitchTimer.ElapsedMilliseconds < 200)
                return;

            //If the previous client is still connected, let them know they are no longer
            //the input client
            if (inputClient != null && inputClient.IsConnected)
                inputClient.NotifyActiveClient(false);

            ISServerSocket oldClient = inputClient;
            inputClient = ISServerSocket.Localhost;

            //enable local input
            inputMan.SetInputBlocked(false);

            clientSwitchTimer.Restart();
            outMan.ResetKeyStates();
            ddController.HandleClientSwitch(oldClient, ISServerSocket.Localhost);
            InputClientSwitched?.Invoke(this, GenerateLocalhostInfo());
        }

        #endregion

        #region localhost events

        private void InputMan_InputReceived(object sender, ISInputData input)
        {
            if (inputClient != ISServerSocket.Localhost)
            {
                if (inputClient.IsConnected)
                {
                    if (inputClient.UdpEnabled && udpHost != null)
                        udpHost.SendInput(input, inputClient);
                    else
                        inputClient.SendInputData(input.ToBytes());
                }
            }
        }

        /// <summary>
        /// Occurs when the cursor hits the edge of the local virtual screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayMan_EdgeHit(object sender, Edge e)
        {
            if (inputClient != ISServerSocket.Localhost)
                return;

            ISServerSocket target = ISServerSocket.Localhost.GetClientAtEdge(e);

            if (target == null || !target.IsConnected)
            {
                ISServerSocket.Localhost.SetClientAtEdge(e, null);
                return;
            }

            if (target != null && target.IsConnected)
                SwitchToClientInput(target.ClientId);
            else
                ISServerSocket.Localhost.SetClientAtEdge(e, null);

        }

        /// <summary>
        /// Fired by the inputmanager when a function hotkey is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputMan_FunctionHotkeyPressed(object sender, Input.Hotkeys.Hotkeyfunction e)
        {
            if (e == Hotkeyfunction.StopServer)
            {
                Stop();
            }
            else if (e == Hotkeyfunction.SendSas)
            {
                if (!inputClient.IsLocalhost)
                    inputClient.SendInputData(new ISInputData(ISInputCode.IS_SENDSAS, 0, 0).ToBytes());
            }
        }

        /// <summary>
        /// Fired by the inputmanager when a hotkey associated with a client is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="targetClient"></param>
        private void InputMan_ClientHotkeyPressed(object sender, Guid targetClient)
        {
            if (targetClient == Guid.Empty)
                SwitchToLocalInput();
            else
                SwitchToClientInput(targetClient);
        }


        private void DisplayMan_DisplayConfigChanged(object sender, DisplayConfig conf)
        {
            ISServerSocket.Localhost.DisplayConfiguration = conf;
        }


        #endregion

        #region client events

        /// <summary>
        /// Fired by the ISClientListener when a client has connected and sent initial info.
        /// We call client.AcceptClient() here to let the client know that it has properly connected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClientListener_ClientConnected(object sender, ISClientListener.ClientConnectedArgs e)
        {
            ISServerSocket client = e.Socket;
            try
            {
                clientMan.AddClient(client);
            }catch(Exception ex)
            {
                if(ex is ClientManager.DuplicateGuidException)
                    ISLogger.Write("client {0} declined: {1}", client.ClientName, "Duplicate GUID");
                else if(ex is ClientManager.DuplicateNameException)
                    ISLogger.Write("client {0} declined: {1}", client.ClientName, "Duplicate name");
                else if(ex is ClientManager.DuplicateNameException)
                    ISLogger.Write("client {0} declined: {1}", client.ClientName, "client limit reached");

                client.DeclineClient(ISServerSocket.ClientDeclinedReason.DuplicateName);
                //todo - possible race condition here? messages need to be sent before the client is disposed
                client.Dispose();
            }

            ISLogger.Write("Server: {1} connected as {0}", e.ClientName, client.ClientEndpoint);
            CreateClientEventHandlers(client);

            client.DisplayConfiguration = new DisplayConfig(e.DisplayConfig);
            client.AcceptClient();

            if (!startArgs.HasArg(StartArguments.NoUdp))
            {
                if (udpHost.SocketBound)
                    udpHost.InitClient(client);
            }
            else
            {
                client.SetUdpEnabled(false);
            }


            ClientConnected?.Invoke(this, GenerateClientInfo(client, true));
        }

        /// <summary>
        /// Assigns event handlers for a client
        /// </summary>
        /// <param name="socket"></param>
        private void CreateClientEventHandlers(ISServerSocket socket)
        {
            socket.ClientDisplayConfigUpdated += Client_ClientDisplayConfigUpdated;
            socket.ConnectionError += Client_ConnectionError;
            socket.ClipboardDataReceived += cbController.OnClientClipboardChange;
            socket.EdgeHit += Socket_EdgeHit;
            socket.RequestedStreamRead += fileController.Client_RequestedStreamRead;
            socket.RequestedFileToken += Socket_RequestedFileToken;
            socket.DragDropDataReceived += ddController.OnClientDataDropped;
            socket.DragDropCancelled += ddController.OnClientDropCancelled;
            socket.DragDropSuccess += ddController.OnClientDropSuccess;
        }

        /// <summary>
        /// Called when a client requests an access token to access a clipboard or dragdrop operation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void Socket_RequestedFileToken(object sender, NetworkSocket.FileTokenRequestArgs args)
        {
            ISServerSocket client = sender as ISServerSocket;
            Guid token;
            //We need to find the dataoperation that the client requested access too (if it exists)
            ServerDataOperation op = null;

            //Check if the specified operation is the active clipboard or dragdrop operation
            if (cbController.CurrentOperation != null && cbController.CurrentOperation.OperationGuid == args.DataOperationId)
                op = cbController.CurrentOperation;
            else if (ddController.CurrentOperation != null && ddController.CurrentOperation.OperationGuid == args.DataOperationId)
                op = ddController.CurrentOperation;
               

            //If we can't find the operation, let the client know
            if(op == null || op.Data.DataType != Clipboard.DataTypes.ClipboardDataType.File)
            {
                client.SendFileErrorResponse(args.NetworkMessageId, "Data operation not found");
                return;
            }

            //If localhost is the host of the operation, we can just create an access token using FileAccessController
            //otherwise, we need to get a token from the host client
            if (op.Host == ISServerSocket.Localhost)
            {
                //We want the token to timeout if it is not access for 10 seconds. The token is only generated for a client
                //when it pastes the files, so having a timeout will ensure that all filestreams are closed when the client
                //has finished pasting all files
                token = fileController.CreateTokenForOperation(op, 10000);
            }
            else
            {
                try
                {
                    token = await op.Host.RequestFileTokenAsync(op.OperationGuid);
                    fileController.AddRemoteAccessToken(op.Host, token);
                }catch(Exception ex)
                {
                    client.SendFileErrorResponse(args.NetworkMessageId, "Failed to get access token from remote client: " + ex.Message);
                    return;
                }
            }

            client.SendTokenRequestReponse(args.NetworkMessageId, token);
        }

        private void Socket_EdgeHit(object sender, Edge edge)
        {
            ISServerSocket client = sender as ISServerSocket;

            if (client != inputClient)
                return;

            ISServerSocket target = client.GetClientAtEdge(edge);

            if (target == ISServerSocket.Localhost)
            {
                SwitchToLocalInput();
            }
            else if (target != null && target.IsConnected)
            {
                SwitchToClientInput(target.ClientId);
            }
            else
            {
                client.SetClientAtEdge(edge, null);
            }
        }

        private void Client_ConnectionError(object sender, string error)
        {
            ISServerSocket client = sender as ISServerSocket;
            if (client == inputClient)
                SwitchToLocalInput();

            try
            {
                clientMan.RemoveClient(client);
            }
            catch (Exception) { }


            ISLogger.Write("Server: Connection error on {0}: {1}", client.ClientName, error);

            if (inputMan.GetClientHotkey(client.ClientId) != null)
                inputMan.RemoveClientHotkey(client.ClientId);
            RemoveClientFromEdges(client);
            ClientDisconnected?.Invoke(this, GenerateClientInfo(client));
            client.Dispose();
        }

        private void RemoveClientFromEdges(ISServerSocket client)
        {
            foreach (var c in clientMan.AllClients.ToArray())
            {
                if (c == client)
                    continue;

                if (c.RightClient == client)
                    c.RightClient = null;
                if (c.LeftClient == client)
                    c.LeftClient = null;
                if (c.TopClient == client)
                    c.TopClient = null;
                if (c.BottomClient == client)
                    c.BottomClient = null;
            }
        }

        private void RemoveEdgeFromClient(ISServerSocket client, Edge edge)
        {
            switch (edge)
            {
                case Edge.Bottom:
                    client.BottomClient = null;
                    break;
                case Edge.Top:
                    client.TopClient = null;
                    break;
                case Edge.Left:
                    client.LeftClient = null;
                    break;
                case Edge.Right:
                    client.RightClient = null;
                    break;

            }
        }

        private void Client_ClientDisplayConfigUpdated(object sender, DisplayConfig config)
        {
            ISServerSocket client = sender as ISServerSocket;
            ISLogger.Write("Server: Display config changed for {0}", client.ClientName);
            ClientDisplayConfigChanged?.Invoke(this, GenerateClientInfo(client));
        }

        #endregion

        #region API

        public void SetStartArgs(StartOptions options)
        {
            startArgs = options;
        }

        public ClientInfo[] GetAllClients()
        {
            ClientInfo[] info = new ClientInfo[clientMan.ClientCount];
            int index = 0;
            foreach (var client in clientMan.AllClients)
            {
                info[index] = GenerateClientInfo(client, true);
                index++;
            }
            return info;
        }


        /// <summary>
        /// Sets the position of a client relative to another client
        /// </summary>
        /// <param name="clientA"></param>
        /// <param name="sideof"></param>
        /// <param name="clientB"></param>
        private void SetClientEdge(ISServerSocket clientA, Edge sideof, ISServerSocket clientB)
        {
            if (clientA == null || clientB == null)
                throw new ArgumentNullException("client was null");

            if (clientA.ClientName == clientB.ClientName)
            {
                throw new ArgumentException("Cannot set X sideof X");
            }

            clientB.SetClientAtEdge(sideof, clientA);
            clientA.SetClientAtEdge(sideof.Opposite(), clientB);
            ISLogger.Write("Server: Set {0} {1}of {2}", clientA.ClientName, sideof, clientB.ClientName);
            clientA.SendClientEdgesUpdate();
            clientB.SendClientEdgesUpdate();
        }

        public void SetMouseInputMode(MouseInputMode mode, int interval = 0)
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            inputMan.SetMouseInputMode(mode, interval);
        }

        /// <summary>
        /// Sets a client to the edge of another client
        /// </summary>
        /// <param name="clientA"></param>
        /// <param name="sideOf"></param>
        /// <param name="clientB"></param>
        /// <exception cref="ArgumentException">The client cannot be found</exception>
        public void SetClientEdge(ClientInfo clientA, Edge sideOf, ClientInfo clientB)
        {
            if(clientA == ClientInfo.None)
            {
                RemoveClientEdge(clientB, sideOf);
                return;
            }

            ISServerSocket cA = clientMan.GetClientFromInfo(clientA);
            ISServerSocket cB = clientMan.GetClientFromInfo(clientB);

            if (cA == null)
                throw new ArgumentException("Invalid clientA");
            if (cB == null)
                throw new ArgumentException("Invalid clientB");

            SetClientEdge(cA, sideOf, cB);
            ClientInfoUpdated?.Invoke(this, GenerateClientInfo(cB));
        }
        public void RemoveClientEdge(ClientInfo client, Edge side)
        {
            ISServerSocket cA = clientMan.GetClientFromInfo(client);
            if (cA == null)
                throw new ArgumentException("Invalid client");

            RemoveEdgeFromClient(cA, side);
            ClientInfoUpdated?.Invoke(this, GenerateClientInfo(cA));
        }

        public FunctionHotkey GetHotkeyForFunction(Hotkeyfunction function)
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            return inputMan.GetFunctionHotkey(function);
        }

        public Hotkey GetHotkeyForClient(ClientInfo client)
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            return inputMan.GetClientHotkey(client.Id);
        }

        public void SetHotkeyForClient(ClientInfo client, Hotkey key)
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            inputMan.AddUpdateClientHotkey(new ClientHotkey(key.Key, key.Modifiers, client.Id));

            ISServerSocket c = clientMan.GetClientById(client.Id);
            c.CurrentHotkey = key;
            ClientInfoUpdated?.Invoke(this, GenerateClientInfo(c, true));
        }

        public void SetHotkeyForFunction(Hotkey key, Hotkeyfunction function)
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            inputMan.AddUpdateFunctionHotkey(new FunctionHotkey(key.Key, key.Modifiers, function));
        }

        private ClientInfo GenerateClientInfo(ISServerSocket client, bool includeEdges = false)
        {
            if (client == ISServerSocket.Localhost)
            {
                return GenerateLocalhostInfo();
            }

            ClientHotkey hk = inputMan.GetClientHotkey(client.ClientId);
            DisplayConfig dConf = client.DisplayConfiguration;

            ClientInfo info = new ClientInfo(client.ClientName, client.ClientId, dConf, hk, client.ClientEndpoint, client.UdpEnabled);
            info.InputClient = inputClient == client;

            if (includeEdges)
            {
                AssignClientEdges(info, client);
            }

            return info;
        }

        public ClientInfo GetLocalhost(){
            return GenerateLocalhostInfo();
        }

        private ClientInfo GenerateLocalhostInfo()
        {
            ISServerSocket local = ISServerSocket.Localhost;
            ClientInfo info = new ClientInfo(local.ClientName, local.ClientId, local.DisplayConfiguration,
                inputMan.GetClientHotkey(local.ClientId), new IPEndPoint(IPAddress.Parse("127.0.0.1"), BoundAddress.Port), local.UdpEnabled);

            info.InputClient = inputClient == local;

            AssignClientEdges(info, local);
            return info;
        }

        private void AssignClientEdges(ClientInfo info, ISServerSocket client)
        {
            info.BottomClient = ClientInfo.None;
            info.RightClient = ClientInfo.None;
            info.LeftClient = ClientInfo.None;
            info.TopClient = ClientInfo.None;

            if (client.BottomClient != null)
                info.BottomClient = GenerateClientInfo(client.BottomClient);
            if (client.TopClient != null)
                info.TopClient = GenerateClientInfo(client.TopClient);
            if (client.RightClient != null)
                info.RightClient = GenerateClientInfo(client.RightClient);
            if (client.LeftClient != null)
                info.LeftClient = GenerateClientInfo(client.LeftClient);
        }

        public void SetClientUdpEnabled(ClientInfo client, bool udpEnabled)
        {
            var c = clientMan.GetClientFromInfo(client);

            if (c == null)
                throw new ArgumentException("Client " + client.Name + " not found");

            if (startArgs.HasArg(StartArguments.NoUdp))
            {
                ISLogger.Write("Ignoring SetClientUdp: NoUdp arg was passed");
                return;
            }

            c.SetUdpEnabled(udpEnabled);
            ClientInfoUpdated?.Invoke(this, GenerateClientInfo(c));
        }

        private ClientInfo GetCurrentInputClient()
        {
            return GenerateClientInfo(inputClient, true);
        }

        public CurrentClipboardData GetGlobalClipboardData()
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            if (cbController.CurrentOperation == null)
                return new CurrentClipboardData(CurrentClipboardData.ClipboardDataType.None, GenerateLocalhostInfo(), DateTime.Now);

            return new CurrentClipboardData((CurrentClipboardData.ClipboardDataType)cbController.CurrentOperation.Data.DataType, GenerateClientInfo(cbController.CurrentOperation.Host), DateTime.Now);
        }

        #endregion
    }
}
