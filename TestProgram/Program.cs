using InputshareLib;
using InputshareLib.Client;
using System;

namespace TestProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            ISClient c = new ISClient();
            c.StartAsync();
            Console.ReadLine();
        }
    }
}
