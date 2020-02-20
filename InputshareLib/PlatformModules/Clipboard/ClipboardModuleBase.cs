﻿using InputshareLib.Clipboard;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.PlatformModules.Clipboard
{
    /// <summary>
    /// Base class for a clipboard module.
    /// 
    /// Monitors the clipboard for changes and can set data on the clipboard
    /// </summary>
    public abstract class ClipboardModuleBase : PlatformModuleBase
    {
        public abstract event EventHandler<ClipboardData> ClipboardChanged;
        public abstract Task SetClipboardAsync(ClipboardData cbData);
    }
}
