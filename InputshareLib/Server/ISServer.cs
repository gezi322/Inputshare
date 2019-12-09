using System;
using System.Collections.Generic;
using InputshareLib.Clipboard;
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

namespace InputshareLib.Server
{
    public sealed class ISServer
    {
        public event EventHandler Started;
        public event EventHandler Stopped;
        public event EventHandler<ClientInfo> ClientConnected;
        public event EventHandler<ClientInfo> ClientDisconnected;
        public event EventHandler<ClientInfo> InputClientSwitched;
        public event EventHandler ClientConfigUpdate;

        public bool Running { get; private set; }

        //OS dependant abstractions
        private DisplayManagerBase displayMan;
        private InputManagerBase inputMan;
        private DragDropManagerBase dragDropMan;
        private OutputManagerBase outMan;
        private ClipboardManagerBase cbManager;

        private ISClientListener clientListener;
        private ClientManager clientMan;
        private ISUdpServer udpHost;

        private GlobalDragDropController ddController;
        private GlobalClipboardController cbController;
        private FileAccessController fileController;
        private GlobalInputController inputController;

        private ISServerSocket inputClient = ISServerSocket.Localhost;
        private StartOptions startArgs;

        public ISServer()
        {
            ISServerSocket.Localhost.ClientEdgeUpdated += (object o, Edge e) => { if (Running) OnClientEdgeChanged(ISServerSocket.Localhost, e); };
            ISServerSocket.Localhost.HotkeyChanged += (object o, EventArgs e) => { if (Running) Client_HotkeyChanged(ISServerSocket.Localhost, ISServerSocket.Localhost.CurrentHotkey); };
        }

        public void Start(ISServerDependencies dependencies, StartOptions args, int port)
        {
            if (Running)
                throw new InvalidOperationException("Server already running");

            ISLogger.EnableConsole = true;

            ISLogger.Write("Starting server...");
            Running = true;
            clientMan = new ClientManager(16);
            startArgs = args;
            StartUdpHost(args, port);
            StartModules(args, dependencies);
            clientListener = new ISClientListener(port);
            AssignLocalEvents();
            SetDefaultHotkeys();
            clientMan.AddClient(ISServerSocket.Localhost);
            ClientConfig.ReloadClientConfigs(clientMan);
            
            Started?.Invoke(this, null);
        }

        public void Stop()
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            try
            {
                ISLogger.Write("Stopping server...");
                StopModules();

                foreach (var client in clientMan.AllClients)
                    client.Dispose();

                clientListener?.Stop();
                udpHost?.Dispose();
            }catch(Exception ex)
            {
                ISLogger.Write("Exception while stopping server!");
                ISLogger.Write(ex.Message);
                ISLogger.Write(ex.StackTrace);
            }
            finally
            {
                Running = false;
                Stopped?.Invoke(this, null);
            }
        }

        private void StartModules(StartOptions args, ISServerDependencies dependencies)
        {
            cbManager = args.HasArg(StartArguments.NoClipboard) ? new NullClipboardManager() : dependencies.ClipboardManager;
            dragDropMan = args.HasArg(StartArguments.NoDragDrop) ? new NullDragDropManager() : dependencies.DragDropManager;
            inputMan = dependencies.InputManager;
            outMan = dependencies.OutputManager;
            displayMan = dependencies.DisplayManager;

            cbController = new GlobalClipboardController(cbManager, clientMan);
            ddController = new GlobalDragDropController(dragDropMan, clientMan);
            fileController = new FileAccessController();
            inputController = new GlobalInputController(clientMan, inputMan, udpHost);

            cbManager.Start();
            dragDropMan.Start();
            inputMan.Start();
            outMan.Start();
            displayMan.Start();
        }

        private void StopModules()
        {
            if (cbManager.Running)
                cbManager.Stop();
            if (dragDropMan.Running)
                dragDropMan.Stop();
            if (inputMan.Running)
                inputMan.Stop();
            if (outMan.Running)
                outMan.Stop();
            if (displayMan.Running)
                displayMan.Stop();

            fileController?.DeleteAllTokens();
        }

        private void StartUdpHost(StartOptions args, int bindPort)
        {
            if (args.HasArg(StartArguments.NoUdp))
                return;

            udpHost = new ISUdpServer(clientMan, bindPort);
        }

        private void AssignLocalEvents()
        {
            inputMan.FunctionHotkeyPressed += HandleFunctionHotkey;
            inputMan.ClientHotkeyPressed += (object o, Guid id) => inputController.HandleClientHotkey(id);
            inputMan.InputReceived += (object o, ISInputData d) => inputController.HandleInputReceived(d);
            displayMan.EdgeHit += (object o, Edge e) => inputController.HandleEdgeHit(ISServerSocket.Localhost, e);
            displayMan.DisplayConfigChanged += DisplayMan_DisplayConfigChanged;
            clientListener.ClientConnected += HandleClientConnected;
            inputController.InputClientSwitched += InputController_InputClientSwitched;
        }

        private ISServerSocket oldInputClient = ISServerSocket.Localhost;
        private void InputController_InputClientSwitched(object sender, ISServerSocket newClient)
        {
            ddController.HandleClientSwitch(oldInputClient, newClient);
            oldInputClient = newClient;
            outMan.ResetKeyStates();
            InputClientSwitched?.Invoke(this, new ClientInfo(newClient, clientMan));
        }

        private void DisplayMan_DisplayConfigChanged(object sender, Displays.DisplayConfig e)
        {
            ISServerSocket.Localhost.DisplayConfiguration = e;
        }

        private void HandleFunctionHotkey(object o, Hotkeyfunction function)
        {
            if (function == Hotkeyfunction.StopServer)
                Stop();
            else if (function == Hotkeyfunction.SendSas)
                if (!inputController.CurrentInputClient.IsLocalhost)
                    inputController.CurrentInputClient.SendInputData(new ISInputData(ISInputCode.IS_SENDSAS, 0, 0).ToBytes());
        }

        private void HandleClientConnected(object o, ISClientListener.ClientConnectedArgs args)
        {
            ISServerSocket client = args.Socket;

            if (!TryAcceptClient(client))
                return;

            client.DisplayConfiguration = new Displays.DisplayConfig(args.DisplayConfig);
            client.AcceptClient();
            AssignClientEvents(client);

            if (!startArgs.HasArg(StartArguments.NoUdp))
            {
                if (udpHost.SocketBound)
                    udpHost.InitClient(client);
            }
            else
            {
                client.SetUdpEnabled(false);
            }

            ClientConfig.ReloadClientConfigs(clientMan);
            ClientConnected?.Invoke(this, new ClientInfo(client, clientMan));
        }

        private bool TryAcceptClient(ISServerSocket client)
        {
            try
            {
                clientMan.AddClient(client);
                return true;
            }
            catch (Exception ex)
            {
                if (ex is ClientManager.DuplicateGuidException)
                    ISLogger.Write("client {0} declined: {1}", client.ClientName, "Duplicate GUID");
                else if (ex is ClientManager.DuplicateNameException)
                    ISLogger.Write("client {0} declined: {1}", client.ClientName, "Duplicate name");
                else if (ex is ClientManager.DuplicateNameException)
                    ISLogger.Write("client {0} declined: {1}", client.ClientName, "client limit reached");

                client.DeclineClient(ISServerSocket.ClientDeclinedReason.DuplicateName);
                //todo - possible race condition here? messages need to be sent before the client is disposed
                client.Dispose();
                return false;
            }
        }

        private void AssignClientEvents(ISServerSocket client)
        {
            //client.ClientDisplayConfigUpdated 
            client.ConnectionError += (object o, string e) => HandleClientDisconnect(client);
            client.ClipboardDataReceived += cbController.OnClientClipboardChange;
            client.EdgeHit += (object o, Edge edge) => inputController.HandleEdgeHit(client, edge);
            client.RequestedStreamRead += fileController.Client_RequestedStreamRead;
            client.RequestedFileToken += Client_RequestedFileToken;
            client.DragDropDataReceived += ddController.OnClientDataDropped;
            client.DragDropCancelled += ddController.OnClientDropCancelled;
            client.DragDropCancelled += ddController.OnClientDropSuccess;
            client.HotkeyChanged += (object o, EventArgs e) => Client_HotkeyChanged(client, client.CurrentHotkey);
            client.ClientEdgeUpdated += (object o, Edge e) => OnClientEdgeChanged(client, e);
        }

        private void OnClientEdgeChanged(ISServerSocket client, Edge e)
        {
            /*
            //Set the opposite edge without causing stack overflow
            switch (e) {
                case Edge.Bottom:
                    client.GetClientAtEdge(e).TopClient = client;
                    break;
                case Edge.Right:
                    client.GetClientAtEdge(e).LeftClient = client;
                    break;
                case Edge.Left:
                    client.GetClientAtEdge(e).RightClient = client;
                    break;
                case Edge.Top:
                    client.GetClientAtEdge(e).BottomClient = client;
                    break;
            }
            */

            ClientConfig.SaveClientEdge(client, e);
            OnClientSettingChanged();
            client.SendClientEdgesUpdate();
        }

        private void OnClientSettingChanged()
        {
            ClientConfigUpdate?.Invoke(this, null);
        }

        private void Client_HotkeyChanged(ISServerSocket client, Hotkey hk)
        {
            try
            {
                inputMan.AddUpdateClientHotkey(new ClientHotkey(hk.Key, hk.Modifiers, client.ClientId));
                OnClientSettingChanged();
            }catch(Exception ex)
            {
                ISLogger.Write("Failed to set hotkey: " + ex.Message);
            }
        }

        private async void Client_RequestedFileToken(object sender, NetworkSocket.FileTokenRequestArgs args)
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
            if (op == null || op.Data.DataType != Clipboard.DataTypes.ClipboardDataType.File)
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
                }
                catch (Exception ex)
                {
                    client.SendFileErrorResponse(args.NetworkMessageId, "Failed to get access token from remote client: " + ex.Message);
                    return;
                }
            }

            client.SendTokenRequestReponse(args.NetworkMessageId, token);
        }

        private void HandleClientDisconnect(ISServerSocket client)
        {
            if (inputController.CurrentInputClient == client)
                inputController.SetInputClient(ISServerSocket.Localhost);

            clientMan.RemoveClient(client);
            ISLogger.Write("Client {0} lost connection", client.ClientName);

            //Remove the clients hotkey if it exists
            if (inputMan.GetClientHotkey(client.ClientId) != null)
                inputMan.RemoveClientHotkey(client.ClientId);

            ClientDisconnected?.Invoke(this, new ClientInfo(client, clientMan));
            client.Dispose();
        }

        private void SetDefaultHotkeys()
        {
            try
            {
                HotkeyModifiers mods = HotkeyModifiers.Alt | HotkeyModifiers.Ctrl | HotkeyModifiers.Shift;
                inputMan.AddUpdateFunctionHotkey(new Input.Hotkeys.FunctionHotkey(WindowsVirtualKey.Q, mods, Input.Hotkeys.Hotkeyfunction.StopServer));
                inputMan.AddUpdateFunctionHotkey(new FunctionHotkey(WindowsVirtualKey.P, HotkeyModifiers.Alt | HotkeyModifiers.Ctrl, Hotkeyfunction.SendSas));
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to set default hotkeys: " + ex.Message);
            }
        }

        public List<ClientInfo> GetAllClients()
        {
            List<ClientInfo> info = new List<ClientInfo>();
            foreach (var client in clientMan.AllClients)
            {
                info.Add(new ClientInfo(client, clientMan));
            }

            return info;
        }

        public ClientInfo GetLocalhost()
        {
            return new ClientInfo(ISServerSocket.Localhost, clientMan);
        }

        public Hotkey GetHotkey(Hotkeyfunction function)
        {
            Hotkey hk = inputMan.GetFunctionHotkey(function);
            return hk == null ? new Hotkey(0, 0) : hk;
        }

        public void SetHotkey(Hotkeyfunction function, Hotkey hk)
        {
            try
            {
                inputMan.AddUpdateFunctionHotkey(new FunctionHotkey(hk.Key, hk.Modifiers, function));
            }catch(Exception ex)
            {
                ISLogger.Write("Failed to set hotkey for {0}: {1}", function, ex.Message);
            }
        }
    }
}
 