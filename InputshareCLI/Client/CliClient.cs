using InputshareLib.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InputshareCLI.Client
{
    internal class CliClient
    {
        private ISClient _client;

        internal async Task Run(string[] args)
        {
            _client = new ISClient();
            await _client.StartAsync();
            object p = await _client.ConnectAsync(IPEndPoint.Parse("127.0.0.1:1234"));
        }
    }
}
