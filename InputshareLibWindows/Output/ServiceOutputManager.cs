using InputshareLib.Input;
using InputshareLib.Output;
using InputshareLibWindows.IPC.AnonIpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.Output
{
    class ServiceOutputManager : IOutputManager
    {
        private AnonIpcHost host;

        public ServiceOutputManager(AnonIpcHost iHostMain)
        {
            host = iHostMain;
        }

        public void ResetKeyStates()
        {
            Send(new ISInputData(ISInputCode.IS_RELEASEALL, 0, 0));
        }

        public void Send(ISInputData input)
        {
            if (host.Connected)
                host.SendInput(input);
        }
    }
}
