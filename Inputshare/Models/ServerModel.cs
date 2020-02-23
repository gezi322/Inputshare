using Avalonia.Threading;
using Inputshare.Common.Input.Hotkeys;
using Inputshare.Common.Input.Keys;
using Inputshare.Common.Server;
using Inputshare.Common.Server.Display;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Models
{
    public class ServerModel
    {
        public event EventHandler Started;
        public event EventHandler Stopped;
        public event EventHandler<ServerDisplayModel> DisplayAdded;
        public event EventHandler<ServerDisplayModel> DisplayRemoved;

        public ObservableCollection<ServerDisplayModel> Displays { get; } = new ObservableCollection<ServerDisplayModel>();
        

        public bool Running { get => _server.Running; }
        public IPEndPoint BindAddress { get => _server.BoundAddress; }
        private ISServer _server;

        public ServerModel()
        {
            _server = new ISServer();
            _server.Displays.DisplayAdded += OnServerDisplayAdded;
            _server.Displays.DisplayRemoved += OnServerDisplayRemoved;
           
        }

        

        private void OnServerDisplayAdded(object sender, DisplayBase display)
        {
            PostToUiThread(() => {
                var model = new ServerDisplayModel(display, Displays);
                Displays.Add(model);
                DisplayAdded?.Invoke(this, model);
            });
        }

        private void OnServerDisplayRemoved(object sender, DisplayBase display)
        {
            PostToUiThread(() => {
                var model = new ServerDisplayModel(display, Displays);
                Displays.Remove(model);
                DisplayRemoved?.Invoke(this, model);
            });
        }

        public async Task StartAsync(int bindPort)
        {
            try
            {
                await _server.StartAsync(new IPEndPoint(IPAddress.Any, bindPort));
                Started?.Invoke(this, null);
            }
            catch (Exception)
            {
                Stopped?.Invoke(this, null);
            }
            
        }

        public async Task StopAsync()
        {
            Displays.Clear();
            await _server.StopAsync();
            Stopped?.Invoke(this, null);
        }

        private void PostToUiThread(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(action);
            }
        }
    }
}
