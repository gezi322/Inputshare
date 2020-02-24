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

        public async Task StopIfRunningAsync()
        {
            if (Running)
                await StopAsync();
        }

        public async Task StartIfNotRunningAsync()
        {
            if (!Running)
                await StartAsync();
        }

        public async Task StartAsync()
        {
            if (Running)
                throw new InvalidOperationException("Module " + ModuleName + " already running");

             await OnStart();
            Running = true;
            Logger.Debug($"Module {ModuleName} started");
        }

        public async Task StopAsync()
        {
            if (!Running)
                throw new InvalidOperationException("Module " + ModuleName + " is not running");

            await OnStop();
            Running = false;
            Logger.Debug($"Module {ModuleName} stopped");
        }

        protected abstract Task OnStart();

        protected abstract Task OnStop();
    }
}
