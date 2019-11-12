using InputshareLib.Input;

namespace InputshareLib.PlatformModules.Output
{
    public abstract class OutputManagerBase : PlatformModuleBase
    {
        public abstract void Send(ISInputData input);

        public abstract void ResetKeyStates();
    }
}
