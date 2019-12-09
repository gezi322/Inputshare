using InputshareLib.Input;
using InputshareLib.PlatformModules.Output;

namespace InputshareLibWindows.PlatformModules.Output
{
    class ServiceOutputManager : OutputManagerBase
    {
        private IpcHandle host;

        public ServiceOutputManager(IpcHandle iHostMain)
        {
            host = iHostMain;
        }

        public override void ResetKeyStates()
        {
            Send(new ISInputData(ISInputCode.IS_RELEASEALL, 0, 0));
        }

        public override void Send(ISInputData input)
        {
            host.host.SendInput(input);
        }

        protected override void OnStart()
        {

        }

        protected override void OnStop()
        {

        }
    }
}
