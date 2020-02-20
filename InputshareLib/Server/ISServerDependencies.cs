using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

        public static ISServerDependencies GetX11Dependencies()
        {
            return new ISServerDependencies
            {
                ClipboardModule = new NullClipboardModule(),
                InputModule = new NullInputModule(),
                OutputModule = new NullOutputModule(),
            };
        }

        /// <summary>
        /// Gets the server dependencies for the current platform
        /// </summary>
        /// <returns></returns>
        public static ISServerDependencies GetCurrentOSDependencies()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetWindowsDependencies();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetX11Dependencies();
            else
                throw new PlatformNotSupportedException();
        }

        private ISServerDependencies()
        {

        }
    }
}
