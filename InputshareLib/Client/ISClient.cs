using InputshareLib.Client.Config;
using InputshareLib.Clipboard;
using InputshareLib.Input;
using InputshareLib.Net.Client;
using InputshareLib.Net.RFS;
using InputshareLib.Net.RFS.Client;
using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Client
{
    public sealed class ISClient
    {
        public bool Running { get; private set; }
        public string ClientName { get; private set; }
        public bool Connected => _socket.State == ClientSocketState.Connected;

        public event EventHandler<string> Disconnected;

        private InputModuleBase InputModule => _dependencies.InputModule;
        private OutputModuleBase OutputModule => _dependencies.OutputModule;
        private ClipboardModuleBase ClipboardModule => _dependencies.ClipboardModule;

        private ISClientDependencies _dependencies;
        private ClientSocket _socket;
        private RFSController _fileController;
        private SideStates _sideStates;
        private bool _isInputClient;

        /// <summary>
        /// Starts the inputshare client instance with the default dependencies for this platform
        /// </summary>
        /// <returns></returns>
        public Task StartAsync()
        {
            return StartAsync(ISClientDependencies.GetCurrentOSDependencies());
        }

        /// <summary>
        /// Starts the inputshare client instance
        /// </summary>
        /// <param name="dependencies"></param>
        /// <returns></returns>
        public async Task StartAsync(ISClientDependencies dependencies)
        {
            if (Running)
                throw new InvalidOperationException("Client already running");

            if (ClientConfig.TryGetLastClientName(out var name))
                ClientName = name;

            _dependencies = dependencies;
            _fileController = new RFSController();
            _socket = new ClientSocket(_fileController);
            _isInputClient = false;
            _sideStates = default;
            await StartModulesAsync();
            AssignSocketEvents();
            AssignModuleEvents();
            Running = true;
        }

        /// <summary>
        /// Connects to a server with the specified name
        /// </summary>
        /// <param name="address"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<bool> ConnectAsync(IPEndPoint address)
        {
            if (Connected) throw new InvalidOperationException("Client already connected");
            if (!Running) throw new InvalidOperationException("Client not running");

            if (ClientName == null)
                ClientName = GenClientName();

            ClientConfig.TrySaveLastAddress(address);

            return await _socket.ConnectAsync(new ClientConnectArgs(address, ClientName, Guid.NewGuid(), InputModule.VirtualDisplayBounds));
        }

        public void SetClientName(string name)
        {
            ClientName = name;
            ClientConfig.TrySaveLastClientName(name);
            Logger.Write($"Client name set to {name}");
        }

        private string GenClientName()
        {
            if (ClientConfig.TryGetLastClientName(out string name))
                return name;
            else
                return Environment.MachineName;
        }

        /// <summary>
        /// Disconnects from the server
        /// </summary>
        public void Disconnect()
        {
            if (!Connected) throw new InvalidOperationException("Client not connected");
            if (!Running) throw new InvalidOperationException("Client not running");

            _socket.DisconnectSocket();
        }

        public async Task StopAsync()
        {
            if (!Running)
                throw new InvalidOperationException("Client not running");

            if (_socket.State == ClientSocketState.Connected)
                _socket.DisconnectSocket();

            _socket.Dispose();
            await StopModulesAsync();
            
            Running = false;
        }

        private void AssignSocketEvents()
        {
            _socket.SideStateChanged += OnSideStatesChanged;
            _socket.ClipboardDataReceived += OnClipboardDataReceived;
            _socket.InputClientChanged += OnInputClientChanged;
            _socket.InputReceived += OnInputReceived;
            _socket.Disconnected += (object o, Exception ex) => Disconnected?.Invoke(this, ex.Message);
        }

        private void AssignModuleEvents()
        {
            ClipboardModule.ClipboardChanged += OnLocalClipboardChanged;
            InputModule.DisplayBoundsUpdated += OnLocalDisplayBoundsChanged;
            InputModule.SideHit += _inputModule_SideHit;
        }

        private async void _inputModule_SideHit(object sender, SideHitArgs args)
        {
            if(Connected && _isInputClient && _sideStates.IsDisplayAtSide(args.Side))
            {
                await _socket.SendSideHitAsync(args.Side, args.PosX, args.PosY);
            }
        }

        private async void OnLocalDisplayBoundsChanged(object sender, Rectangle newBounds)
        {
            if (Connected)
            {
                await _socket.SendDisplayUpdateAsync(newBounds);
            }
            
        }

        private async void OnLocalClipboardChanged(object sender, ClipboardData cbData)
        {
            if (Connected)
            {
                if (cbData.IsTypeAvailable(ClipboardDataType.HostFileGroup))
                {
                    string[] files = cbData.GetLocalFiles();
                    var group = _fileController.HostLocalGroup(files);
                    cbData.SetRemoteFiles(group);
                }

                await _socket.SendClipboardDataAsync(cbData);
            }
        }

        private void OnInputReceived(object sender, InputData input)
        {
            OutputModule.SimulateInput(ref input);
        }

        private void OnInputClientChanged(object sender, bool state)
        {
            _isInputClient = state;
            InputModule.SetMouseHidden(!state);
        }

        private async void OnClipboardDataReceived(object sender, Clipboard.ClipboardData cbData)
        {
            if (cbData.IsTypeAvailable(ClipboardDataType.RemoteFileGroup))
            {
                var group = cbData.GetRemoteFiles();

                RFSClientFileGroup fg = new RFSClientFileGroup(group.GroupId, group.Files, _socket);
                cbData.SetRemoteFiles(fg);
            }

            await ClipboardModule.SetClipboardAsync(cbData);
        }

        private void OnSideStatesChanged(object sender, ClientSidesChangedArgs e)
        {
            if(Connected)
                _sideStates = new SideStates(e.Left, e.Right, e.Top, e.Bottom);
        }

        private async Task StartModulesAsync()
        {
            await InputModule.StartIfNotRunningAsync();
            await OutputModule.StartIfNotRunningAsync();
            await ClipboardModule.StartIfNotRunningAsync();
        }

        private async Task StopModulesAsync()
        {
            await InputModule.StopIfRunningAsync();
            await OutputModule.StopIfRunningAsync();
            await ClipboardModule.StopIfRunningAsync();
        }

        
    }
}
