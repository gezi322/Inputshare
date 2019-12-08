using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace InputshareLib
{
    public class StartOptions
    {
        public StartArguments Args { get; }
        public IPEndPoint SpecifiedServer { get; private set; }
        public int SpecifiedStartPort { get; private set; }
        public StartOptions(List<string> args) {
            StartArguments validArgs = 0;
            
            foreach(StartArguments possibleArg in (StartArguments[])Enum.GetValues(typeof(StartArguments)))
            {
                foreach (var enteredArg in args)
                {
                    if (enteredArg.ToLower().Contains(possibleArg.ToString().ToLower()))
                    {
                        if(possibleArg == StartArguments.Connect)
                        {
                            if (!ReadAddress(args))
                            {
                                Console.WriteLine("Invalid address specified for connect argument");
                                continue;
                            }
                        }else if(possibleArg == StartArguments.StartPort)
                        {
                            if (!ReadStartPort(args)){
                                Console.WriteLine("Invalid port specified for startport argument");
                                continue;
                            }
                        }

                        validArgs |= possibleArg;
                    }
                }
            }
                

            Args = validArgs;
        }

        private bool ReadAddress(List<string> args)
        {
            int index = args.IndexOf("connect");

            if (index < -1 || args.Count <= index + 1)
                return false;

            Console.WriteLine("ADDRESS = " + args[index + 1]);
            IPEndPoint.TryParse(args[index + 1], out IPEndPoint addr);
            SpecifiedServer = addr == null ? null : addr;
            return SpecifiedServer != null;
        }

        private bool ReadStartPort(List<string> args)
        {
            int index = args.IndexOf("startport");

            if (index == -1 || args.Count <= index + 1)
                return false;

            int.TryParse(args[index+1], out int p);
            SpecifiedStartPort = p;
            return p != 0;
        }

        public bool HasArg(StartArguments arg)
        {
            return Args.HasFlag(arg);
        }

        public StartArguments[] GetArgs()
        {
            List<StartArguments> retArgs = new List<StartArguments>();

            foreach (StartArguments a in (StartArguments[])Enum.GetValues(typeof(StartArguments)))
                if (Args.HasFlag(a))
                    retArgs.Add(a);

            return retArgs.ToArray();
        }
        
        public static void PrintHelp()
        {
            Console.WriteLine("USAGE: Inputshare server|client [options]");

            Console.WriteLine("    'noudp' - Disables udp port binding and uses TCP instead");
            Console.WriteLine("    'nodragdrop - Disables drag/drop functionality");
            Console.WriteLine("    'noclipboard' - Disables hooking and setting of the local clipboard (server still keeps a global clipboard)");
            Console.WriteLine("    'verbose' - Prints debugging information");
            Console.WriteLine("    'connect address:port - Connects to the specified server");
            Console.WriteLine("    'autoreconnect' - Keeps trying to connect or reconnect to the specified server");
            Console.WriteLine("    'startport port' - Starts the server on the specified port");
        }

        public void PrintArgs()
        {
            foreach(var arg in Enum.GetValues(typeof(StartArguments)))
            {
                if (Args.HasFlag((StartArguments)arg))
                    ISLogger.Write(arg);
            }
        }
    }

    [Flags]
    public enum StartArguments
    {
        NoGui = 1,
        Verbose = 2,
        NoUdp = 4,
        NoDragDrop = 8,
        NoClipboard = 16,
        Server = 32,
        Client = 64,
        Help = 128,
        Connect = 256,
        AutoReconnect = 512,
        StartPort = 1024,
        Service = 2048,
    }
}
