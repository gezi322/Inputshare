using Avalonia.Threading;
using Inputshare.Common.Client;
using Inputshare.Common.Net.Broadcast;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Models
{
    public class ClientModel
    {
        public event EventHandler Connected;
        public event EventHandler<string> Disconnected;
        public event EventHandler<BroadcastReceivedArgs> BroadcastedAddressReceived;
        public IPEndPoint ServerAddress { get => _client.ServerAddress; }

        public bool ClientRunning { get => _client.Running; }
        public bool ClientConnected { get => _client.Connected; }
        public string ClientName { get => _client.ClientName; set => _client.ClientName = value; }
        public async Task StopAsync() => await _client.StopAsync();
        public async Task StartAsync() => await _client.StartAsync();

        private readonly ISClient _client = new ISClient();

        public ClientModel()
        {
            _client.Disconnected += OnClientDisconnected;
            _client.ServerBroadcastReceived += OnBroadcastReceived;
        }

        private void OnBroadcastReceived(object sender, BroadcastReceivedArgs args)
        {
            PostToUiThread(() =>
            {
                BroadcastedAddressReceived?.Invoke(this, args);
            });
        }

        private void OnClientDisconnected(object sender, string reason)
        {
            PostToUiThread(() =>
            {
                Disconnected?.Invoke(this, reason);
            });
        }

        public async Task<bool> ConnectAsync(IPEndPoint address)
        {
            bool connected = await _client.ConnectAsync(address);

            if (connected)
                Connected?.Invoke(this, null);

            return connected;
        }

        public void Disconnect()
        {
            _client.Disconnect();
            //ISClient doesn't raise the disconnected event if Disconnect() was called,
            //only raised if connection error occurs
            Disconnected?.Invoke(this, "Disconnected from server");
        }

        

        private void PostToUiThread(Action action)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.InvokeAsync(action);
            }
        }
    }
}
