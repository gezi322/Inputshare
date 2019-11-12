using InputshareLib;
using InputshareLib.Client;
using InputshareLib.Server;

#if WindowsBuild
using InputshareLibWindows;
#endif
using System;

namespace Inputshare
{
    class Program
    {
        private const string address = "192.168.0.12";
        private const int port = 4441;
        private const string clientName = "LinuxDevTest";

        static void Main(string[] args)
        {
            ISLogger.EnableConsole = true;

            Console.WriteLine("S to start server");
            Console.WriteLine("C to start client");

            ConsoleKeyInfo k = Console.ReadKey();

            if (k.Key == ConsoleKey.C)
                InitClient();
            else
                InitServer();

            Console.ReadLine();
        }

        private static void InitClient()
        {
            ISClientDependencies deps;

#if WindowsBuild
             deps = WindowsDependencies.GetClientDependencies();
#else
             deps = ISClientDependencies.GetLinuxDependencies(new InputshareLib.Linux.SharedXConnection());
#endif

            ISClient client = new ISClient(deps);
            client.SetClientName(clientName);
            client.Connect(address, port);
        }

        private static void InitServer()
        {
            ISServerDependencies deps;
            

#if WindowsBuild
            deps = WindowsDependencies.GetServerDependencies();
#else
            deps = ISServerDependencies.GetLinuxDependencies(new InputshareLib.Linux.SharedXConnection());
#endif

            ISServer server = new ISServer(deps);
            server.Start(4441);
        }
    }
}
