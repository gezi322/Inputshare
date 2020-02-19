using InputshareLib.Clipboard;
using InputshareLib.Input;
using InputshareLib.Net.RFS;
using InputshareLib.Net.RFS.Client;
using InputshareLib.Net.Server;
using InputshareLib.PlatformModules;
using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;
using InputshareLib.Server.Config;
using InputshareLib.Server.Display;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Server
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
        public ObservableCollection<DisplayBase> Displays = new ObservableCollection<DisplayBase>();
        internal DisplayBase InputDisplay { get; private set; }
        internal LocalDisplay LocalHostDisplay { get; private set; }
        internal InputModuleBase InputModule => _dependencies.InputModule;
        internal OutputModuleBase OutputModule => _dependencies.OutputModule;
        internal ClipboardModuleBase ClipboardModule => _dependencies.ClipboardModule;

        private ClientListener _listener;
        private ISServerDependencies _dependencies;
        private RFSController _fileController;

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
                _dependencies = dependencies;
                _fileController = new RFSController();
                await StartModulesAsync();
                LocalHostDisplay = new LocalDisplay(_dependencies);
                InputDisplay = LocalHostDisplay;
                OnDisplayAdded(LocalHostDisplay);
                InputModule.InputReceived += OnInputReceived;

                _listener = new ClientListener();
                _listener.ClientConnected += OnClientConnected;
                var listenTask = _listener.ListenAsync(bindAddress, _fileController);
                Running = true;

                await listenTask;
            }catch(Exception ex)
            {
                Logger.Write("Failed to start server: " + ex.Message);
                Logger.Write(ex.StackTrace);
            }
            finally
            {
                if (_listener != null && _listener.Listening)
                    _listener.Stop();

                await StopModulesAsync();
                Running = false;
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

            foreach (var display in Displays)
                if(display != LocalHostDisplay)
                    display.RemoveDisplay();

            _listener.Stop();
            await StopModulesAsync();
            Running = false;
        }

        private async Task StartModulesAsync()
        {
            if (!InputModule.Running)
                await InputModule.StartAsync();
            if (!OutputModule.Running)
                await OutputModule.StartAsync();
            if (!ClipboardModule.Running)
                await ClipboardModule.StartAsync();
        }

        private async Task StopModulesAsync()
        {
            if (InputModule.Running)
                await InputModule.StopAsync();
            if (OutputModule.Running)
                await OutputModule.StopAsync();
            if (ClipboardModule.Running)
                await ClipboardModule.StopAsync();
        }

        /// <summary>
        /// Runs then a client connects.
        /// Creates a display object to represent the client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnClientConnected(object sender, ClientConnectedArgs args)
        {
            //Create a display object and set it up
            var display = new ClientDisplay(args);
            OnDisplayAdded(display);
           
            /*
            if (display.DisplayName == "IPC")
            {
                display.SetDisplayAtSide(Side.Right, LocalHostDisplay);
                LocalHostDisplay.SetDisplayAtSide(Side.Left, display);
            }
            else if (display.DisplayName == "ENVY15")
            {
                display.SetDisplayAtSide(Side.Top, LocalHostDisplay);
                LocalHostDisplay.SetDisplayAtSide(Side.Bottom, display);
            }*/
        }

        /// <summary>
        /// Runs when a the cursor hits the side of a display
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void OnDisplaySideHit(object sender, SideHitArgs args)
        {
            var display = sender as DisplayBase;
            if(InputDisplay == display)
            {
                var target = display.GetDisplayAtSide(args.Side);

                if(target != null)
                {
                    await SetInputDisplayAsync(target, args.Side, args.PosX, args.PosY);
                }
            }
        }

        /// <summary>
        /// Runs whenever a display is disconnected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="display"></param>
        private async void OnDisplayRemoved(object sender, DisplayBase display)
        {
            Logger.Write($"Removed display {display.DisplayName}");
            Displays.Remove(display);
            RemoveReferences(display);

            //If the display that was removed was the input display, switch back to local input
            if (display == InputDisplay)
                await SetInputDisplayAsync(LocalHostDisplay);
        }

        /// <summary>
        /// Runs each time a new display is connected
        /// </summary>
        /// <param name="display"></param>
        private void OnDisplayAdded(DisplayBase display)
        {
            display.DisplayRemoved += OnDisplayRemoved;
            display.SideHit += OnDisplaySideHit;
            display.ClipboardChanged += OnDisplayClipboardChanged;

            Displays.Add(display);
            ReloadConfiguration();
        }

        private async void OnDisplayClipboardChanged(object sender, ClipboardData cbData)
        {
            Logger.Write("Server: Clipboard changed!"); 

            if(sender == LocalHostDisplay && cbData.IsTypeAvailable(ClipboardDataType.HostFileGroup))
            {
                string[] files = cbData.GetLocalFiles();
                var group = _fileController.HostFiles(files);
                cbData.SetRemoteFiles(group);
            }else if(sender != LocalHostDisplay && cbData.IsTypeAvailable(ClipboardDataType.HostFileGroup))
            {
                var group = cbData.GetRemoteFiles();
                RFSClientFileGroup fg = new RFSClientFileGroup(group.GroupId, group.Files, (sender as ClientDisplay).Socket);
                cbData.SetRemoteFiles(fg);
            }

            foreach(var display in Displays.Where(i => i != sender))
            {
                await display.SetClipboardAsync(cbData);
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
        internal async Task SetInputDisplayAsync(DisplayBase display)
        {
            if (!Displays.Contains(display))
            {
                Logger.Write($"Can't switch to {display.DisplayName}: Not in display list");
                RemoveReferences(display);
                return;
            }

            await display.NotfyInputActiveAsync();
            await InputDisplay.NotifyClientInvactiveAsync();
            InputDisplay = display;
            Logger.Write($"Input display: {display.DisplayName}");
        }

        /// <summary>
        /// Switches input to the specified display and moves the cursor to the correct position
        /// on the target display
        /// </summary>
        /// <param name="display"></param>
        /// <param name="side"></param>
        /// <param name="hitX"></param>
        /// <param name="hitY"></param>
        internal async Task SetInputDisplayAsync(DisplayBase display, Side side, int hitX, int hitY)
        {
            var newPos = CalculateCursorPosition(display, side, hitX, hitY);
            var input = new InputData(InputCode.MouseMoveAbsolute, (short)newPos.X, (short)newPos.Y);
            display.SendInput(ref input);
            await SetInputDisplayAsync(display);
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
            switch (side) {
                case Side.Top:
                    return new Point(hitX, newDisplay.DisplayBounds.Bottom - 2);
                case Side.Right:
                    return new Point(newDisplay.DisplayBounds.Left + 2, hitY);
                case Side.Left:
                    return new Point(newDisplay.DisplayBounds.Right - 2, hitY);
                case Side.Bottom:
                    return new Point(hitX, newDisplay.DisplayBounds.Top + 2);
                default:
                    return new Point(0, 0);
            }
        }

        /// <summary>
        /// Reloads configurations for all displays
        /// </summary>
        private void ReloadConfiguration()
        {
            foreach(var display in Displays)
            {
                foreach (Side side in Extensions.AllSides)
                {
                    if(DisplayConfig.TryReadProperty(display, side.ToString(), out var dis)){
                        var target = GetDisplay(dis);

                        if(target != null)
                        {
                            display.SetDisplayAtSide(side, target);
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
