using Inputshare.Common.Input;
using Inputshare.Common.Net.RFS;
using Inputshare.Common.Net.Server;
using Inputshare.Common.PlatformModules;
using Inputshare.Common.PlatformModules.Clipboard;
using Inputshare.Common.PlatformModules.Input;
using Inputshare.Common.PlatformModules.Output;
using Inputshare.Common.Server.Config;
using Inputshare.Common.Server.Display;
using System;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Inputshare.Common.Server
{
    /// <summary>
    /// Inputshare server implementation
    /// </summary>
    public sealed class ISServer
    {
        public bool Running { get; private set; }
        public IPEndPoint BoundAddress { get => _listener.BindAddress; }

        /// <summary>
        /// Displays that are connected to the server
        /// </summary>
        public ObservableDisplayList Displays;
        internal DisplayBase InputDisplay { get; private set; }
        internal LocalDisplay LocalHostDisplay { get; private set; }
        internal InputModuleBase InputModule => _dependencies.InputModule;
        internal OutputModuleBase OutputModule => _dependencies.OutputModule;
        internal ClipboardModuleBase ClipboardModule => _dependencies.ClipboardModule;

        private ClientListener _listener;
        private ISServerDependencies _dependencies;
        private RFSController _fileController;
        private GlobalClipboard _clipboardController;
        private object _clientListLock = new object();
        private object _inputClientLock = new object();

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
                Displays = new ObservableDisplayList();
                _dependencies = dependencies;
                _fileController = new RFSController();
                _clipboardController = new GlobalClipboard(Displays, _fileController);
                await StartModulesAsync();
                LocalHostDisplay = new LocalDisplay(_dependencies);
                InputDisplay = LocalHostDisplay;
                OnDisplayAdded(LocalHostDisplay);
                InputModule.InputReceived += OnInputReceived;

                _listener = new ClientListener();
                _listener.ClientConnected += OnClientConnected;
                _listener.BeginListening(bindAddress, _fileController);
                Console.Title = ($"Inputshare server @ {BoundAddress} (LocalHost)");
                Running = true;
                
            }catch(Exception ex)
            {
                if (_listener != null && _listener.Listening)
                    _listener.Stop();

                foreach (var display in Displays.ToArray())
                    if (display != LocalHostDisplay)
                        display.RemoveDisplay();

                Logger.Write("Failed to start server: " + ex.Message);
                Logger.Write(ex.StackTrace);
            }
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

            if (_listener != null && _listener.Listening)
                _listener.Stop();

            foreach (var display in Displays.ToArray())
                if (display != LocalHostDisplay)
                    display.RemoveDisplay();

            _listener.Stop();
            await StopModulesAsync();
            Running = false;
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

        /// <summary>
        /// Runs then a client connects.
        /// Creates a display object to represent the client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnClientConnected(object sender, ClientConnectedArgs args)
        {
            if(Displays.Where(i => i.DisplayName.ToLower() == args.Name.ToLower()).FirstOrDefault() != null)
            {
                Logger.Write($"Removed client {args.Name}: Duplicate client name");
                args.Socket.Dispose();
                return;
            }

            //Create a display object and set it up
            var display = new ClientDisplay(args);
            OnDisplayAdded(display);
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
                Logger.Write($"Removed display {display.DisplayName}");
                Displays.Remove(display);
                RemoveReferences(display);

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

                Displays.Add(display);

                if (display.DisplayName == "IPC")
                {
                    display.SetDisplayAtSide(Side.Right, LocalHostDisplay);
                    LocalHostDisplay.SetDisplayAtSide(Side.Left, display);
                }
                else if (display.DisplayName == "ENVY15")
                {
                    display.SetDisplayAtSide(Side.Top, LocalHostDisplay);
                    LocalHostDisplay.SetDisplayAtSide(Side.Bottom, display);
                }
            }

            ReloadConfiguration();
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
            lock (_inputClientLock)
            {
                if (!Displays.Contains(display))
                {
                    Logger.Write($"Can't switch to {display.DisplayName}: Not in display list");
                    RemoveReferences(display);
                    return;
                }

                display.NotfyInputActive();
                InputDisplay.NotifyClientInvactive();
                InputDisplay = display;
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
            var newPos = CalculateCursorPosition(display, side, hitX, hitY);
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
            lock (_clientListLock)
            {
                foreach (var display in Displays)
                {
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
