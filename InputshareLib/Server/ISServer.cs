using InputshareLib.Input;
using InputshareLib.Net.Server;
using InputshareLib.PlatformModules;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;
using InputshareLib.Server.Display;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
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

        private ClientListener _listener;
        private ISServerDependencies _dependencies;

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

            _dependencies = dependencies;
            await StartModulesAsync();

            _listener = new ClientListener();
            _listener.ClientConnected += OnClientConnected;

            Task.Run(async () => await _listener.ListenAsync(bindAddress));

            LocalHostDisplay = new LocalDisplay(InputModule, OutputModule);
            InputDisplay = LocalHostDisplay;
            OnDisplayAdded(LocalHostDisplay);

            InputModule.InputReceived += OnInputReceived;

            Running = true;
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

            _listener.Stop();
            Displays.Clear();
            await StopModulesAsync();
            Running = false;
        }

        private async Task StartModulesAsync()
        {
            if (!InputModule.Running)
                await InputModule.StartAsync();
            if (!OutputModule.Running)
                await OutputModule.StartAsync();
        }

        private async Task StopModulesAsync()
        {
            if (InputModule.Running)
                await InputModule.StopAsync();
            if (OutputModule.Running)
                await OutputModule.StopAsync();
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

            if(display.DisplayName == "\aa")
            {
                display.SetDisplayAtSide(Side.Top, Displays[1]);
                Displays[1].SetDisplayAtSide(Side.Bottom, display);
            }else if(display.DisplayName == "ENVY15")
            {
                display.SetDisplayAtSide(Side.Right, LocalHostDisplay);
                LocalHostDisplay.SetDisplayAtSide(Side.Left, display);
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
                {
                    SetInputDisplay(target, args.Side, args.PosX, args.PosY);
                }
            }
        }

        /// <summary>
        /// Runs whenever a display is disconnected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="display"></param>
        private void OnDisplayRemoved(object sender, DisplayBase display)
        {
            Displays.Remove(display);
            RemoveReferences(display);

            //If the display that was removed was the input display, switch back to local input
            if (display == InputDisplay)
                SetInputDisplay(LocalHostDisplay);
        }

        private void OnDisplayAdded(DisplayBase display)
        {
            display.DisplayRemoved += OnDisplayRemoved;
            display.SideHit += OnDisplaySideHit;
            Displays.Add(display);
        }

        private void RemoveReferences(DisplayBase display)
        {
            //Remove any reference to the display
            foreach (var dis in Displays)
            {
                foreach (Side side in (Side[])Enum.GetValues(typeof(Side)))
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
            if (!Displays.Contains(display))
            {
                Logger.Write($"Can't switch to {display.DisplayName}: Not in display list");
                RemoveReferences(display);
                return;
            }


            InputDisplay.SetInputInactive();
            display.SetInputActive();
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
        internal void SetInputDisplay(DisplayBase display, Side side, int hitX, int hitY) 
        {
            SetInputDisplay(display);
            var newPos = CalculateCursorPosition(display, side, hitX, hitY);
            var input = new InputData(InputCode.MouseMoveAbsolute, (short)newPos.X, (short)newPos.Y);
            display.SendInput(ref input);
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
    }
}
