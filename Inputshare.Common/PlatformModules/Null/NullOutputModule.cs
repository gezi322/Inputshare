using Inputshare.Common.Input;
using Inputshare.Common.PlatformModules.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Common.PlatformModules.Null
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
