using InputshareLib.Input;
using InputshareLib.Net.Client;
using InputshareLib.Net.Server;
using InputshareLib.PlatformModules;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;
using InputshareLib.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib
{
    public class TEST
    {
        public TEST()
        {
            //Task.Run(() => RunServer());
            Task.Run(() => RunClient());
            Console.ReadLine();
        }

        public async void RunClient()
        {
            ClientSocket soc = new ClientSocket();
            await outMod.StartAsync();
            soc.InputReceived += Soc_InputReceived;
            await soc.ConnectAsync(IPEndPoint.Parse("192.168.0.17:1234"));
            soc.Disconnected += Soc_Disconnected;
            Console.ReadLine();
        }

        private void Soc_Disconnected(object sender, Exception e)
        {
            Logger.Write("DISCONNECTED: " + e.Message);
        }

        WindowsOutputModule outMod = new WindowsOutputModule();
        int i = 0;
        private void Soc_InputReceived(object sender, Input.InputData e)
        {
            outMod.SimulateInput(ref e);
        }

        public async void RunServer()
        {
            ISServer s = new ISServer();
            await s.StartAsync(new ISServerDependencies { InputModule = new WindowsInputModule() }, 1234);
        }

        private void Listener_ClientConnected(object sender, ClientConnectedArgs e)
        {
            
            e.Socket.Disconnected += Socket_Disconnected;
        }

        private void Socket_Disconnected(object sender, ServerSocket e)
        {
            Logger.Write($"{e.Address} Disconnected...");
        }
    }
}
