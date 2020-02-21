using Inputshare.CLI.Client;
using Inputshare.CLI.Server;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Inputshare
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if(args.Length < 1)
            {
                PrintArgs();
                return;
            }

            if(string.Compare(args[0], "-server", true) == 0)
            {
                if(string.Compare(args[1], "-startport", true) == 0)
                {
                    if(int.TryParse(args[2], out var port))
                    {
                        await new CLIServer().Run(port);
                    }
                    else
                    {
                        PrintArgs();
                        return;
                    }
                }
                else
                {
                    PrintArgs();
                    return;
                }
            }else if(string.Compare(args[0], "-client", true) == 0)
            {
                if(string.Compare(args[1], "-connect", true) == 0)
                {
                    if(IPEndPoint.TryParse(args[2], out var address))
                    {
                        await new CLIClient().Run(address);
                    }
                    else
                    {
                        PrintArgs();
                        return;
                    }
                }
                else
                {
                    PrintArgs();
                    return;
                }
            }
        }
        
        static void PrintArgs()
        {
            Console.WriteLine("Usage:");

            Console.WriteLine("Inputshare -server -startport 1234");
            Console.WriteLine("Inputshare -client -connect address:port");
        }
    }
}
