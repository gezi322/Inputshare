using InputshareLib.Clipboard;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.PlatformModules.Clipboard
{
    public class NullClipboardModule : ClipboardModuleBase
    {
        public override event EventHandler<ClipboardData> ClipboardChanged;

        public override Task SetClipboardAsync(ClipboardData cbData)
        {
            return Task.CompletedTask;
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
