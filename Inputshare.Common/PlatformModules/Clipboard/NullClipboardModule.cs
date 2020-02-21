using Inputshare.Common.Clipboard;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Common.PlatformModules.Clipboard
{
    public class NullClipboardModule : ClipboardModuleBase
    {
#pragma warning disable CS0067
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

#pragma warning restore CS0067

    }
}
