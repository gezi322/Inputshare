using System;

namespace InputshareLib.PlatformModules
{
    public abstract class PlatformModuleBase
    {
        public bool Running { get; private set; }
        public string ModuleName { get; }

        public PlatformModuleBase()
        {
            ModuleName = this.GetType().Name;
        }

        public void Start()
        {
            if (Running)
                throw new InvalidOperationException(ModuleName + " already running");

            Running = true;
            OnStart();

            ISLogger.Write("Module {0} started", ModuleName);
        }

        public void Stop()
        {
            if (!Running)
                throw new InvalidOperationException(ModuleName + " not running");

            Running = false;
            OnStop();

            ISLogger.Write("Module {0} stopped", ModuleName);
        }

        protected abstract void OnStart();
        protected abstract void OnStop();
    }
}
