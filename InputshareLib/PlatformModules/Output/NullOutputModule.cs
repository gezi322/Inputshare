using InputshareLib.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.PlatformModules.Output
{
    public class NullOutputModule : OutputModuleBase
    {
        public override void SimulateInput(ref InputData input)
        {

        }

        protected override Task OnStart()
        {
            return Task.CompletedTask;
        }

        protected override Task OnStop()
        {
            return Task.CompletedTask;
        }
    }
}
