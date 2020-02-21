using Inputshare.Common.PlatformModules;
using Inputshare.Common.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Server
{
    internal sealed class CLIServer
    {
        private ISServer _server;

        internal async Task Run(string[] args)
        {
            _server = new ISServer();
            Console.WriteLine("Starting...");
            await _server.StartAsync(ISServerDependencies.GetCurrentOSDependencies(), new System.Net.IPEndPoint(IPAddress.Any, 1234));

        }
    }
}
