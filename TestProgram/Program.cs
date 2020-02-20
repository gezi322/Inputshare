using InputshareLib;
using InputshareLib.Client;
using InputshareLib.Server.Display;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using InputshareLib.PlatformModules.Windows.Native;


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
            await _client.StartAsync();
        }
        static bool HandleConsoleExit(Kernel32.CtrlTypes type)
        {
            Process.GetCurrentProcess().Kill();
            return true;

        }

    }
}
