using Inputshare.Common.PlatformModules.Base;
using Inputshare.Common.PlatformModules.Linux;
using Inputshare.Common.PlatformModules.Linux.Modules;
using Inputshare.Common.PlatformModules.Windows.Modules;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Inputshare.Common.PlatformModules
{
    /// <summary>
    /// Contains modules required to run inputshare server
    /// </summary>
    public class ISServerDependencies : IDisposable
    {
        public InputModuleBase InputModule { get; private set; }
        public OutputModuleBase OutputModule { get; private set; }
        public ClipboardModuleBase ClipboardModule { get; private set; }
        private IPlatformDependency[] _pDependencies = new IPlatformDependency[0];

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
            var xCon = new XConnection();

            return new ISServerDependencies
            {
                ClipboardModule = new X11ClipboardModule(xCon),
                InputModule = new X11InputModule(xCon),
                OutputModule = new X11OutputModule(xCon),
                _pDependencies = new IPlatformDependency[] {xCon}
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

        public string[] GetModuleNames()
        {
            return new string[]
            {
                 InputModule.GetType().Name,
                OutputModule.GetType().Name,
                ClipboardModule.GetType().Name,
            };
        }

        private ISServerDependencies()
        {

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var dep in _pDependencies)
                        dep.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
