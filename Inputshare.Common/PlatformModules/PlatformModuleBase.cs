using Inputshare.Common.PlatformModules.Windows;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Common.PlatformModules
{
    /// <summary>
    /// Base class for platform specific modules (clipboard, input hooks, simulating user input etc)
    /// </summary>
    public abstract class PlatformModuleBase
    {
        public string ModuleName { get => this.GetType().Name; }
        public bool Running { get; private set; }

        internal async Task StopIfRunningAsync()
        {
            if (Running)
                await StopAsync();
        }

        internal async Task StartIfNotRunningAsync()
        {
            if (!Running)
                await StartAsync();
        }

        internal async Task StartAsync()
        {
            if (Running)
                throw new InvalidOperationException("Module " + ModuleName + " already running");

             await OnStart();
            Running = true;
            //Logger.Write($"Module {ModuleName} started");
        }

        internal async Task StopAsync()
        {
            if (!Running)
                throw new InvalidOperationException("Module " + ModuleName + " is not running");

            await OnStop();
            Running = false;
            //Logger.Write($"Module {ModuleName} stopped");
        }

        protected abstract Task OnStart();

        protected abstract Task OnStop();
    }
}
