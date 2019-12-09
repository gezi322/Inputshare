using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using InputshareLib;

namespace Inputshare.Cli
{
    public class CliMain
    {
        private StartOptions args;

        public CliMain(StartOptions options)
        {
            args = options;

            Console.WriteLine("Starting CLI mode");

            if (args.HasArg(StartArguments.Client))
                StartCliClient();
            else if (args.HasArg(StartArguments.Server))
                StartCliServer();
            else
            {
                StartOptions.PrintHelp();
            }
        }

        void StartCliClient()
        {
            Console.WriteLine("Starting CLI client");

            if(args.SpecifiedServer == null)
            {
                Console.WriteLine("A valid connect argument must be used to connect to a server");
                return;
            }

            new CliClient(args);
        }

        void StartCliServer()
        {
            Console.WriteLine("Starting CLI server");

            if(args.SpecifiedStartPort == 0)
            {
                Console.WriteLine("A valid startport argument must be used to start server");
                return;
            }

            new CliServer(args);
        }
    }
}
