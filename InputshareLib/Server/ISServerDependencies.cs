using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.PlatformModules
{
    /// <summary>
    /// Contains modules required to run inputshare server
    /// </summary>
    public class ISServerDependencies
    {
        public InputModuleBase InputModule { get; private set; }
        public OutputModuleBase OutputModule { get; private set; }
        public ClipboardModuleBase ClipboardModule { get; private set; }

        public static ISServerDependencies GetWindowsDependencies()
        {
            return new ISServerDependencies
            {
                ClipboardModule = new WindowsClipboardModule(),
                InputModule = new WindowsInputModule(),
                OutputModule = new WindowsOutputThreadedModule()
            };
        }

        private ISServerDependencies()
        {

        }
    }
}
