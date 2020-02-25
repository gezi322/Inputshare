using Inputshare.Common.Input;
using Inputshare.Common.Input.Hotkeys;
using Inputshare.Common.Net.Broadcast;
using Inputshare.Common.Net.RFS;
using Inputshare.Common.Net.Server;
using Inputshare.Common.Net.UDP;
using Inputshare.Common.PlatformModules;
using Inputshare.Common.PlatformModules.Base;
using Inputshare.Common.Server.Config;
using Inputshare.Common.Server.Display;
using System;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Inputshare.Common.Server
{
    /// <summary>
    /// Inputshare server implementation
    /// </summary>
    public sealed class ISServer
    {
        public event EventHandler Started;
        public event EventHandler Stopped;

        public bool Running { get; private set; }
        public IPEndPoint BoundAddress { get => _listener.BindAddress; }

        /// <summary>
        /// Displays that are connected to the server
        /// </summary>
        public ObservableDisplayList Displays = new ObservableDisplayList();

        private DisplayBase InputDisplay;
        private LocalDisplay LocalHostDisplay;
        private InputModuleBase InputModule => _dependencies.InputModule;
        private OutputModuleBase OutputModule => _dependencies.OutputModule;
        private ClipboardModuleBase ClipboardModule => _dependencies.ClipboardModule;

        private ClientListener _listener;
        private ISServerDependencies _dependencies;
        private RFSController _fileController;
        private GlobalClipboard _clipboardController;
        private readonly object _clientListLock = new object();
        private readonly object _inputClientLock = new object();
        private ServerUdpSocket _udpHost;
        private BroadcastSender _broadcaster;

        /// <summary>
        /// Starts the inputshare server with the default dependencies for this platform
        /// </summary>
        /// <param name="bindAddress"></param>
        /// <returns></returns>
        public Task StartAsync(IPEndPoint bindAddress)
        {
            return StartAsync(ISServerDependencies.GetCurrentOSDependencies(), bindAddress);
        }

        /// <summary>
        /// Starts the inputshare server
        /// </summary>
        /// <param name="dependencies">platform specific dependencies</param>
        /// <param name="bindAddress">Address to bind network socket</param>
        /// <returns></returns>
        public async Task StartAsync(ISServerDependencies dependencies, IPEndPoint bindAddress)
        {
            if (Running)
                throw new InvalidOperationException("Server already running");

            try
            {
                Logger.Information($"Starting server with modules:\n {string.Join('\n', dependencies.GetModuleNames())}");
                ServerConfig.LoadConfig();
                _dependencies = dependencies;
                _fileController = new RFSController();
                _clipboardController = new GlobalClipboard(Displays, _fileController);
                await StartModulesAsync();
                CreateLocalhostDisplay();
                InputModule.InputReceived += OnInputReceived;
                CreateClientListener(bindAddress);
                CreateBroadcastHost(bindAddress.Port);
                CreateUdpHost(bindAddress.Port);
                Started?.Invoke(this, null);
                Running = true;
                Logger.Information($"Server started at address {bindAddress}");
                
            }catch(Exception ex)
            {
                await StopAsync();

                Logger.Error("Failed to start server: " + ex.Message);
                Logger.Error(ex.StackTrace);
                throw ex;
            }
        }

        private void CreateBroadcastHost(int serverBindPort)
        {
            if (ServerConfig.BroadcastEnabled)
                _broadcaster = BroadcastSender.Create(2000, serverBindPort, ServerConfig.BroadcastPort, "0.0.0.10");
            else
                Logger.Debug($"Disabling broadcasting");
        }

        private void CreateUdpHost(int bindPort)
        {
            if (ServerConfig.BindUDP)
                _udpHost = ServerUdpSocket.Create(bindPort);
            else
                Logger.Debug($"Disabling udp socket");
        }

        private void CreateClientListener(IPEndPoint bindAddress)
        {
            Logger.Verbose($"Creating TCP client listener");
            _listener = new ClientListener();
            _listener.ClientConnected += OnClientConnected;
            _listener.BeginListening(bindAddress, _fileController);
        }

        private void CreateLocalhostDisplay()
        {
            LocalHostDisplay = new LocalDisplay(_dependencies, Displays);
            InputDisplay = LocalHostDisplay;
            OnDisplayAdded(LocalHostDisplay);

            //Create a stop hotkey
            var mods = KeyModifiers.Alt | KeyModifiers.Ctrl | KeyModifiers.Shift;
            InputModule.RegisterHotkey(new Hotkey(Input.Keys.WindowsVirtualKey.Q, mods), async() => {
                Logger.Information("Stop hotkey pressed");
                await StopAsync();
            });
        }

        private void OnInputReceived(object sender, InputData e)
        {
            InputDisplay?.SendInput(ref e);
        }

        /// <summary>
        /// Stops the inputshare server
        /// </summary>
        public async Task StopAsync()
        {
            if (!Running)
                throw new InvalidOperationException("Server is not running");

            Logger.Information($"Stopping server");

            if (_listener != null && _listener.Listening)
                _listener.Stop();

            foreach (var display in Displays.ToArray())
                if (display != LocalHostDisplay)
                    display.RemoveDisplay();

            Displays.Clear();
            _broadcaster?.Dispose();
            _udpHost?.Dispose();
            _listener?.Stop();
            await StopModulesAsync();
            _fileController?.Dispose();
            _clipboardController?.Dispose();
            _dependencies?.Dispose();
            Logger.Information($"Server stopped");
            Stopped?.Invoke(this, null);
            Running = false;
        }

        private async Task StartModulesAsync()
        {
            Logger.Verbose($"Starting modules");
            await InputModule.StartIfNotRunningAsync();
            await OutputModule.StartIfNotRunningAsync();
            await ClipboardModule.StartIfNotRunningAsync();
        }

        private async Task StopModulesAsync()
        {
            Logger.Verbose($"Stopping modules");
            await InputModule?.StopIfRunningAsync();
            await OutputModule?.StopIfRunningAsync();
            await ClipboardModule?.StopIfRunningAsync();
        }

        /// <summary>
        /// Runs then a client connects.
        /// Creates a display object to represent the client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnClientConnected(object sender, ClientConnectedArgs args)
        {
            DisplayBase display = null;

            try
            {
                if (Displays.Where(i => i.DisplayName.ToLower() == args.Name.ToLower()).FirstOrDefault() != null)
                {
                    Logger.Warning($"Removed client {args.Name}: Duplicate client name");
                    args.Socket.Dispose();
                    return;
                }

                //Create a display object and set it up
                display = new ClientDisplay(Displays, args);

                if (_udpHost != null && args.UdpPort != 0)
                    args.Socket.SetUdpSocket(_udpHost, new IPEndPoint(args.Socket.Address.Address, args.UdpPort));

                OnDisplayAdded(display);
            }catch(Exception ex)
            {
                if (display != null)
                    display.RemoveDisplay();

                Logger.Verbose($"An error occurred at OnClientConnect: {ex.Message}");
            }
            
        }

        /// <summary>
        /// Runs when a the cursor hits the side of a display
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnDisplaySideHit(object sender, SideHitArgs args)
        {
            var display = sender as DisplayBase;
            if(InputDisplay == display)
            {
                var target = display.GetDisplayAtSide(args.Side);

                if(target != null)
                    SetInputDisplay(target, args.Side, args.PosX, args.PosY);
            }
        }

        /// <summary>
        /// Runs whenever a display is disconnected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="display"></param>
        private void OnDisplayRemoved(object sender, DisplayBase display)
        {
            lock (_clientListLock)
            {
                if (display is ClientDisplay cDisplay)
                    _udpHost?.RemoveHandlersForAddress(cDisplay.Socket.UdpAddress);

                Logger.Information($"Removed display {display.DisplayName}");
                Displays.Remove(display);
                RemoveReferences(display);

                if (display.Hotkey != null)
                    if (InputModule.IsHotkeyInUse(display.Hotkey))
                        InputModule.RemoveHotkey(display.Hotkey);

                //If the display that was removed was the input display, switch back to local input
                if (display == InputDisplay)
                    SetInputDisplay(LocalHostDisplay);
            }

           
        }

        /// <summary>
        /// Runs each time a new display is connected
        /// </summary>
        /// <param name="display"></param>
        private void OnDisplayAdded(DisplayBase display)
        {
            lock (_clientListLock)
            {
                display.DisplayRemoved += OnDisplayRemoved;
                display.SideHit += OnDisplaySideHit;
                display.HotkeyChanging += OnDisplayHotkeyChanging;
                display.HotkeyChanged += OnDisplayHotkeyChanged;
                Displays.Add(display);
            }

            ReloadConfiguration();
        }

        /// <summary>
        /// Runs before a displays hotkey is changed and unregisters the 
        /// old hotkey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="hk"></param>
        private void OnDisplayHotkeyChanging(object sender, Hotkey hk)
        {
            if (InputModule.IsHotkeyInUse(hk))
                InputModule.RemoveHotkey(hk);
        }

        private void OnDisplayHotkeyChanged(object sender, Hotkey hk)
        {
            try
            {
                var display = sender as DisplayBase;
                Logger.Debug($"Setting hotkey for {display.DisplayName} to {hk.ToString()}");

                if (hk != Hotkey.None)
                {
                    InputModule.RegisterHotkey(hk, new Action(() => {
                        SetInputDisplay(display);
                    }));

                    Logger.Information($"Set hotkey for {display} to {hk}");
                }
                else
                {
                    Logger.Information("Cleared hotkey for " + display);
                }

                display.Hotkey = hk;
                DisplayConfig.TrySaveClientHotkey(display, hk);
            }catch(Exception ex)
            {
                Logger.Error($"Failed to set hotkey: {ex.Message}");
            }
            
        }

        /// <summary>
        /// Removes the specified display from all other displays
        /// </summary>
        /// <param name="display"></param>
        private void RemoveReferences(DisplayBase display)
        {
            //Remove any reference to the display
            foreach (var dis in Displays)
            {
                foreach (Side side in Extensions.AllSides)
                {
                    if (dis.GetDisplayAtSide(side) == display)
                    {
                        dis.RemoveDisplayAtSide(side);
                    }
                }
            }
        }

        /// <summary>
        /// Switches input to the specified display
        /// </summary>
        /// <param name="display"></param>
        internal void SetInputDisplay(DisplayBase display)
        {
            if(InputDisplay == display)
                return;

            lock (_inputClientLock)
            {
                Logger.Debug($"Setting input display to {display.DisplayName}");
                if (!Displays.Contains(display))
                {
                    Logger.Verbose($"Can't switch to {display.DisplayName}: Not in display list");
                    RemoveReferences(display);
                    return;
                }

                display.NotfyInputActive();
                InputDisplay.NotifyClientInvactive();
                InputDisplay = display;
                Logger.Debug($"Input display set to {display.DisplayName}");
            }
        }

        /// <summary>
        /// Switches input to the specified display and moves the cursor to the correct position
        /// on the target display
        /// </summary>
        /// <param name="display"></param>
        /// <param name="side"></param>
        /// <param name="hitX"></param>
        /// <param name="hitY"></param>
        internal void SetInputDisplay(DisplayBase display, Side side, int hitX, int hitY)
        {
            Logger.Debug($"Calculating cursor position switching to {display} from side {side} of {InputDisplay}");
            var newPos = CalculateCursorPosition(display, side, hitX, hitY);
            Logger.Debug($"Setting position: {newPos.X}:{newPos.Y}");
            var input = new InputData(InputCode.MouseMoveAbsolute, (short)newPos.X, (short)newPos.Y);
            display.SendInput(ref input);
            SetInputDisplay(display);
        }

        /// <summary>
        /// Calculates where the cursor should be when switching input displays
        /// </summary>
        /// <param name="side"></param>
        /// <param name="hitX"></param>
        /// <param name="hitY"></param>
        /// <returns></returns>
        private Point CalculateCursorPosition(DisplayBase newDisplay, Side side, int hitX, int hitY)
        {
            return side switch
            {
                Side.Top => new Point(hitX, newDisplay.DisplayBounds.Bottom - 2),
                Side.Right => new Point(newDisplay.DisplayBounds.Left + 2, hitY),
                Side.Left => new Point(newDisplay.DisplayBounds.Right - 2, hitY),
                Side.Bottom => new Point(hitX, newDisplay.DisplayBounds.Top + 2),
                _ => new Point(0, 0),
            };
        }

        /// <summary>
        /// Reloads configurations for all displays
        /// </summary>
        private void ReloadConfiguration()
        {
            Logger.Debug("Reloading configuration");

            lock (_clientListLock)
            {
                foreach (var display in Displays)
                {
                    if (DisplayConfig.TryGetClientHotkey(display, out var hk))
                        display.SetHotkey(hk);

                    foreach (Side side in Extensions.AllSides)
                    {
                        if (DisplayConfig.TryGetClientAtSide(display, side, out var clientName))
                        {
                            var target = GetDisplay(clientName);

                            if (target != null)
                            {
                                display.SetDisplayAtSide(side, target);
                            }
                        }
                    }
                }
            }
        }

        private DisplayBase GetDisplay(string name)
        {
            return Displays.Where(i => i.DisplayName == name).FirstOrDefault();
        }
    }
}
