using InputshareLib.PlatformModules;
using InputshareLib.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InputshareCLI.Server
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
