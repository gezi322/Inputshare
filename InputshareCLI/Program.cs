using InputshareCLI.Client;
using InputshareCLI.Server;
using System;
using System.Threading.Tasks;

namespace InputshareCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Console.WriteLine("Press C to run client");
            Console.WriteLine("Press S to run server");

            ConsoleKeyInfo key = Console.ReadKey();

            if (key.Key == ConsoleKey.S)
                Task.Run(async () => await new CLIServer().Run(args));
            else if (key.Key == ConsoleKey.C)
                Task.Run(async () => await new CliClient().Run(args));
            else
                return;

            Console.ReadLine();
        }
    }
}
