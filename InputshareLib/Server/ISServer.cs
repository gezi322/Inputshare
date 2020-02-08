using InputshareLib.Net.Server;
using InputshareLib.PlatformModules;
using InputshareLib.PlatformModules.Windows;
using System;
using System.Collections.Generic;
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

        public async Task StartAsync(ISServerDependencies dependencies, int port)
        {
            _dependencies = dependencies;
            _clients = new List<ServerSocket>();
            _listener = new ClientListener();
            _listener.ClientConnected += OnClientConnected;

            var listenTask = _listener.StartAsync(new IPEndPoint(IPAddress.Any, port));
            await StartModulesAsync();

            _dependencies.InputModule.InputReceived += InputModule_InputReceived;
            _dependencies.InputModule.SideHit += InputModule_SideHit;

            _dependencies.InputModule.SetInputRedirected(true);
            await Task.Delay(5000);
            _dependencies.InputModule.SetInputRedirected(false);


            await listenTask;
        }

        private void InputModule_SideHit(object sender, Side e)
        {
            Logger.Write($"Hit side {e}");
        }

        private void InputModule_InputReceived(object sender, Input.InputData e)
        {
            if(_clients.Count > 0)
                _clients[0].SendInput(e);
        }

        private void OnClientConnected(object sender, ClientConnectedArgs e)
        {
            _clients.Add(e.Socket);
            e.Socket.Disconnected += Socket_Disconnected;
        }

        private void Socket_Disconnected(object sender, ServerSocket e)
        {
            _clients.Remove(e);
            Logger.Write($"{e.Address} disconnected");
        }

        private async Task StartModulesAsync()
        {
            if(!_dependencies.InputModule.Running)
                await _dependencies.InputModule.StartAsync();
        }
    }
}
