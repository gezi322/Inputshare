using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib;
using InputshareLib.Client;

namespace Inputshare.Cli
{
    class CliClient
    {
        private ISClient client;

        public CliClient(StartOptions options)
        {
            client = new ISClient();
            client.Start(options, GetDependencies());
            client.Connected += Client_Connected;
            client.ConnectionError += Client_ConnectionError;
            client.ConnectionFailed += Client_ConnectionFailed;
            client.Disconnected += Client_Disconnected;
            client.Connect(options.SpecifiedServer);
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            Console.WriteLine("Disconnected from server");
        }

        private void Client_ConnectionFailed(object sender, string e)
        {
            Console.WriteLine("Connection failed: " + e);
        }

        private void Client_ConnectionError(object sender, string e)
        {
            Console.WriteLine("Connection error: " + e);
        }

        private void Client_Connected(object sender, System.Net.IPEndPoint e)
        {
            Console.WriteLine("Connected to server");
        }

        private ISClientDependencies GetDependencies()
        {
#if WindowsBuild
            return InputshareLibWindows.WindowsDependencies.GetClientDependencies();
#elif LinuxBuild
            return ISClientDependencies.GetLinuxDependencies();
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
