using InputshareLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareWindows
{
    internal static class ArgumentParser
    {
        internal static LaunchArguments ParseArgs(string[] args)
        {
            LaunchArgs selectedArgs = 0;

            foreach(var arg in args)
            {
                if (arg.ToLower() == "startserver")
                    selectedArgs |= LaunchArgs.StartServer;
                if (arg.ToLower() == "startclient")
                    selectedArgs |= LaunchArgs.StartClient;
                if (arg.ToLower() == "startserviceclient")
                    selectedArgs |= LaunchArgs.StartServiceClient;
            }

            ISLogger.Write("Args: " + selectedArgs);

            return new LaunchArguments(selectedArgs);
        }

        
        internal class LaunchArguments
        {
            public LaunchArgs Args { get;  }

            public LaunchArguments(LaunchArgs args)
            {
                Args = args;
            }
        }

        [Flags]
        internal enum LaunchArgs
        {
            StartServer = 1,
            StartClient = 2,
            StartServiceClient = 4,

        }
    }
}
