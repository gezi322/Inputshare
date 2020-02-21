using Inputshare.Common.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.CLI.Client
{
    public sealed class CLIClient
    {
        private ISClient _client;
        private IPEndPoint _address;

        public async Task Run(IPEndPoint address)
        {
            _client = new ISClient();
            _address = address;

            await _client.StartAsync();
            _client.Disconnected += OnDisconnect;
            while(!await _client.ConnectAsync(_address)) { }
        }

        private async void OnDisconnect(object sender, string reason)
        {
            Console.WriteLine("Disconnected: " + reason);

            while (!await _client.ConnectAsync(_address)) { }
        }
    }
}
