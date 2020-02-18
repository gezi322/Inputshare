using InputshareLib.Clipboard;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.PlatformModules.Clipboard
{
    public abstract class ClipboardModuleBase : PlatformModuleBase
    {
        public abstract event EventHandler<ClipboardData> ClipboardChanged;
        public abstract Task SetClipboardAsync(ClipboardData cbData);
    }
}
