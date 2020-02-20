using InputshareLib;
using InputshareLib.Client;
using InputshareLib.Server.Display;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using InputshareLib.PlatformModules.Windows.Native;
using System.Net;

namespace TestProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () => await Run());
            Console.ReadLine();
        }
        static Kernel32.ConsoleCtrlDelegate _del;
        static ISClient _client;
        static async Task Run()
        {
            _del = new Kernel32.ConsoleCtrlDelegate(HandleConsoleExit);
            Kernel32.SetConsoleCtrlHandler(_del, true);
            _client = new ISClient();
            _client.Disconnected += _client_Disconnected;
            await _client.StartAsync(ISClientDependencies.GetWindowsDependencies());
            _client.SetClientName(Environment.MachineName);
            while(!await _client.ConnectAsync(IPEndPoint.Parse("192.168.0.17:1234"))) { }
        }

        private static async void _client_Disconnected(object sender, string e)
        {
            Logger.Write("Lost connection...");
            while(!await _client.ConnectAsync(IPEndPoint.Parse("192.168.0.17:1234"))) { }
        }

        static bool HandleConsoleExit(Kernel32.CtrlTypes type)
        {
            Process.GetCurrentProcess().Kill();
            return true;

        }

    }
}
