using InputshareLib.Net.Server;
using InputshareLib.PlatformModules;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;
using InputshareLib.PlatformModules.Windows;
using InputshareLib.Server.Displays;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Server
{
    public sealed class ISServer
    {
        public bool Running { get; private set; }

        private ClientListener _listener;
        private ISServerDependencies _dependencies;
        private List<ServerSocket> _clients;

        private DisplayBase _inputDisplay;

        private OutputModuleBase _outModule => _dependencies.OutputModule;
        private InputModuleBase _inputModule => _dependencies.InputModule;

        private readonly List<DisplayBase> _displays = new List<DisplayBase>();

        public async Task StartAsync(ISServerDependencies dependencies, int port)
        {
            _displays.Clear();
            _dependencies = dependencies;
            _clients = new List<ServerSocket>();
            _listener = new ClientListener();
            _listener.ClientConnected += OnClientConnected;
            await StartModulesAsync();
            _inputDisplay = new LocalDisplay(_inputModule, _outModule);
            _displays.Add(_inputDisplay);

            var listenTask = _listener.ListenAsync(new IPEndPoint(IPAddress.Any, port));
           
            _inputModule.InputReceived += InputModule_InputReceived;
            _inputModule.SideHit += InputModule_SideHit;
        }

        public void Stop()
        {

        }

        private void InputModule_SideHit(object sender, SideHitArgs e)
        {
            if (_displays.Count > 1 && (_inputDisplay is LocalDisplay) )
            {
                SetInputDisplay(_displays[0], e.Side, e.PosX, e.PosY); ;
            }
        }

        private void InputModule_InputReceived(object sender, Input.InputData input)
        {
            if(_inputDisplay != null)
                _inputDisplay.SendInput(ref input);
        }

        private void OnClientConnected(object sender, ClientConnectedArgs args)
        {
            _clients.Add(args.Socket);
            var display = new ClientDisplay(args);
            display.SideHit += (object o, SideHitArgs args) => Socket_SideHit1(o, new Tuple<Side, int, int>(args.Side, args.PosX, args.PosY));
            args.Socket.Disconnected += Socket_Disconnected;
            _displays.Add(display);

            if(display.ClientName == "LINX10")
            {
                _displays[0].SetDisplayAtEdge(Side.Bottom, display);
                display.SetDisplayAtEdge(Side.Top, _displays[0]);
            }
            else
            {
                _displays[0].SetDisplayAtEdge(Side.Left, display);
                display.SetDisplayAtEdge(Side.Right, _displays[0]);
            }
        }

        private void Socket_SideHit1(object sender, Tuple<Side, int, int> e)
        {
            if (!(_inputDisplay is LocalDisplay))
            {
                SetInputDisplay(_inputDisplay, e.Item1, e.Item2, e.Item3);
            }
        }


        private void Socket_Disconnected(object sender, ServerSocket e)
        {
            _displays.Remove(_displays[1]);
            _clients.Remove(e);
        }

        private async Task StartModulesAsync()
        {
            if(!_dependencies.InputModule.Running)
                await _dependencies.InputModule.StartAsync();
            if(!_dependencies.OutputModule.Running)
                await _outModule.StartAsync();
        }

        /// <summary>
        /// Changes the input display and moves the mouse to the closest possible position
        /// </summary>
        /// <param name="oldDisplay"></param>
        /// <param name="side"></param>
        /// <param name="mX"></param>
        /// <param name="mY"></param>
        private void SetInputDisplay(DisplayBase oldDisplay, Side side, int mX, int mY)
        {
            if (!oldDisplay.TryGetDisplayAtSide(side, mX, mY, out DisplayBase newDisplay, out int npX, out int npY))
            {
                return;
            }

            oldDisplay.SetNotInputDisplay();
            newDisplay.SetInputDisplay(npX, npY);
            _inputDisplay = newDisplay;
        }

        /// <summary>
        /// Changes the input display
        /// </summary>
        /// <param name="display"></param>
        private void SetInputDisplay(DisplayBase display)
        {
            _inputModule.SetInputRedirected(display is LocalDisplay ? false : true);

            _inputDisplay = display;
        }
    }
}
