using Inputshare.Common.PlatformModules.Clipboard;
using Inputshare.Common.PlatformModules.Input;
using Inputshare.Common.PlatformModules.Output;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Inputshare.Common.Client
{
    public class ISClientDependencies
    {
        public InputModuleBase InputModule { get; private set; }
        public ClipboardModuleBase ClipboardModule { get; private set; }
        public OutputModuleBase OutputModule { get; private set; }

        public static ISClientDependencies GetWindowsDependencies()
        {
            return new ISClientDependencies
            {
                OutputModule = new WindowsOutputThreadedModule(),
                InputModule = new WindowsInputModule(),
                ClipboardModule = new WindowsClipboardModule()
            };
        }

        public static ISClientDependencies GetCurrentOSDependencies()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetWindowsDependencies();

            throw new PlatformNotSupportedException();
        }

        private ISClientDependencies()
        {

        }
    }
}
