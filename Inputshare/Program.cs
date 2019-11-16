using InputshareLib;
using InputshareLib.Client;
using InputshareLib.Server;
using InputshareLib.Linux;
using System.Threading;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Server.API;
using System.Threading.Tasks;

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
        private static string clientName = Environment.MachineName;

        private static SharedXConnection xCon;

        //This is just for testing!
        static void Main(string[] args)
        {
            ISLogger.EnableConsole = true;

#if LinuxBuild
            xCon = new SharedXConnection();
#endif
            if(args[0] == "s"){
                InitServer();
            }else{
                InitClient();
            }
        }

        private static void InitClient()
        {
            ISClientDependencies deps = null;

#if WindowsBuild
             deps = WindowsDependencies.GetClientDependencies(); //TODO
#elif true
            deps = ISClientDependencies.GetLinuxDependencies(xCon);
#endif
            ISClient client = new ISClient(deps);
            client.SasRequested += Client_SasRequested;
            client.SetClientName(clientName);
            client.Connect(address, port);
        }

        private static void Client_SasRequested(object sender, EventArgs e)
        {
            Console.WriteLine("Requested SAS!");
        }

        private static bool InitX()
        {
            try
            {
                xCon = new SharedXConnection();
                return true;
            }catch(XLibException ex)
            {
                ISLogger.Write("Failed to start: " + ex.Message);
                Console.Write("Failed to start: " + ex.Message);
                return false;
            }
        }

        private static void InitServer()
        {
            ISServerDependencies deps;
            

#if WindowsBuild
            deps = WindowsDependencies.GetServerDependencies();
#else
            deps = ISServerDependencies.GetLinuxDependencies(xCon);
#endif
            ISServer server = new ISServer(deps);
            Console.Title = "Inputshare server";
            server.Stopped += OnServerStop;
            server.Started += Server_Started;
            server.ClientConnected += Server_ClientConnected;
            server.ClientDisconnected += Server_ClientDisconnected;
            server.GlobalClipboardContentChanged += Server_GlobalClipboardContentChanged;
            server.InputClientSwitched += Server_InputClientSwitched;
            server.Start(4441);
        }

        private static void Server_Started(object sender, EventArgs e)
        {
            Console.WriteLine("Server started");
        }

        private static void Server_InputClientSwitched(object sender, ClientInfo e)
        {
            Console.Title = string.Format("Inputshare server ({0})", e.Name);
        }

        private static void Server_ClientDisconnected(object sender, ClientInfo e)
        {
            Console.WriteLine("Client {0} connection lost", e.Name);
        }

        private static void Server_GlobalClipboardContentChanged(object sender, CurrentClipboardData e)
        {
            Console.WriteLine("Clipboard content changed by {0} ({1})", e.Host, e.Type);
        }

        private static void Server_ClientConnected(object s, ClientInfo client){

            Task.Run(() => {
                Console.WriteLine("Client {0} connected! set edge", client.Name);
                Console.WriteLine("1) Top");
                Console.WriteLine("2) Bottom");
                Console.WriteLine("3) Left");
                Console.WriteLine("4) Right");

                //really bad idea, blocks caller

                ConsoleKeyInfo k = Console.ReadKey(true);
                Edge e = EdgeFromKey(k.Key);

                if (e == Edge.None)
                    return;

                ((ISServer)s).SetClientEdge(client, e, ((ISServer)s).GetLocalhost());
                Console.WriteLine("Set {0} {1}of {2}", client.Name, e, "Localhost");
            });

        }

        private static Edge EdgeFromKey(ConsoleKey key) => key switch
        {
            ConsoleKey.D1 => Edge.Top,
            ConsoleKey.D2 => Edge.Bottom,
            ConsoleKey.D3 => Edge.Left,
            ConsoleKey.D4 => Edge.Right,
            _ => Edge.None
        };

        private static void OnServerStop(object s, EventArgs e){
            Console.WriteLine("Server exited...");
            xCon.Close();
        }
    }
}
 