using InputshareLib;
using InputshareLib.Client;
using InputshareLib.Server.Display;
using System;
using System.Threading.Tasks;

namespace TestProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () => await Run());
            Console.ReadLine();
        }

        static async Task Run()
        {
            ISClient c = new ISClient();
            await c.StartAsync();
        }
    }
}
