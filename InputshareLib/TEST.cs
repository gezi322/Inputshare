using InputshareLib.Input;
using InputshareLib.Net.Client;
using InputshareLib.Net.Server;
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
            Task.Run(() => RunServer());
            Task.Run(() => RunClient());
            Console.ReadLine();
        }

        public async void RunClient()
        {
            ClientSocket soc = new ClientSocket();
            soc.InputReceived += Soc_InputReceived;
            await soc.ConnectAsync(IPEndPoint.Parse("192.168.0.17:1234"));
            soc.Disconnected += Soc_Disconnected;
            Console.ReadLine();
        }

        private void Soc_Disconnected(object sender, Exception e)
        {
            Logger.Write("DISCONNECTED: " + e.Message);
        }

        int i = 0;
        private void Soc_InputReceived(object sender, Input.InputData e)
        {
            Logger.Write($"INPUT RECEIVED {e.Code}:{e.ParamA}:{e.ParamB} ({i})");
            i++;
        }

        public async void RunServer()
        {
            ClientListener listener = new ClientListener();
            listener.ClientConnected += Listener_ClientConnected;
            var t = listener.StartAsync(new IPEndPoint(IPAddress.Any, 1234));

            

            await t;
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
