using Inputshare.Common.PlatformModules;
using Inputshare.Common.PlatformModules.Clipboard;
using Inputshare.Common.PlatformModules.Input;
using Inputshare.Common.PlatformModules.Linux;
using Inputshare.Common.PlatformModules.Output;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Inputshare.Common.Client
{
    public class ISClientDependencies : IDisposable
    {
        public InputModuleBase InputModule { get; private set; }
        public ClipboardModuleBase ClipboardModule { get; private set; }
        public OutputModuleBase OutputModule { get; private set; }

        private IPlatformDependency[] _pDependencies;


        public static ISClientDependencies GetWindowsDependencies()
        {
            return new ISClientDependencies
            {
                OutputModule = new WindowsOutputThreadedModule(),
                InputModule = new WindowsInputModule(),
                ClipboardModule = new WindowsClipboardModule()
            };
        }

        public static ISClientDependencies GetX11Dependencies()
        {
            var xCon = new XConnection();

            return new ISClientDependencies
            {
                ClipboardModule = new NullClipboardModule(),
                InputModule = new NullInputModule(),
                OutputModule = new X11OutputModule(xCon),
                _pDependencies = new IPlatformDependency[] {xCon}
            };
        }

        public static ISClientDependencies GetCurrentOSDependencies()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetWindowsDependencies();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetX11Dependencies();
            
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

        private ISClientDependencies()
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
