using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib;
using InputshareLib.Client;

namespace Inputshare.Cli
{
    class CliClient
    {
        private ISClient client;

        public CliClient(StartOptions options)
        {
            client = new ISClient();
            client.Start(options, GetDependencies());

            client.Connect(options.SpecifiedServer);
        }

        private ISClientDependencies GetDependencies()
        {
#if WindowsBuild
            return InputshareLibWindows.WindowsDependencies.GetClientDependencies();
#elif LinuxBuild
            return ISClientDependencies.GetLinuxDependencies();
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
