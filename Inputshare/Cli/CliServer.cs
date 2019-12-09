using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib;
using InputshareLib.Server;

namespace Inputshare.Cli
{
    class CliServer
    {
        private ISServer server;

        public CliServer(StartOptions options)
        {
            server = new ISServer();
            server.Start(GetDependencies(), options, options.SpecifiedStartPort);
        }

        private ISServerDependencies GetDependencies()
        {
#if WindowsBuild
            return InputshareLibWindows.WindowsDependencies.GetServerDependencies();
#elif LinuxBuild
            return ISServerDependencies.GetLinuxDependencies();
#else
            throw new PlatformNotSupportedException();
#endif

        }
    }
}
