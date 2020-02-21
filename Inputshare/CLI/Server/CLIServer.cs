using Inputshare.Common.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.CLI.Server
{
    public sealed class CLIServer
    {
        private ISServer _server;

        public async Task Run(int port)
        {
            _server = new ISServer();
            Console.WriteLine("Starting server on port " + port);
            await _server.StartAsync(new IPEndPoint(IPAddress.Any, port));

            while (true)
            {
                Console.Write("Server: ");
                string cmd = Console.ReadLine();

                if (cmd.ToLower() == "stop")
                    await _server.StopAsync();
            }
        }



    }
}
