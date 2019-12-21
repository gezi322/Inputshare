using InputshareLib.Linux;
using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Displays;
using InputshareLib.PlatformModules.DragDrop;
using InputshareLib.PlatformModules.Output;
using System;

namespace InputshareLib.Client
{
    public class ISClientDependencies : IDisposable
    {
        private IDisposable globalDependency;

        public OutputManagerBase outputManager { get; set; }
        public ClipboardManagerBase clipboardManager { get; set; }
        public DisplayManagerBase displayManager { get; set; }
        public DragDropManagerBase dragDropManager { get; set; }

        public static ISClientDependencies GetLinuxDependencies()
        {
            Linux.SharedXConnection xCon = new SharedXConnection();

            return new ISClientDependencies(xCon)
            {
                clipboardManager = new LinuxClipboardManager(xCon),
                displayManager = new LinuxDisplayManager(xCon),
                dragDropManager = new NullDragDropManager(),
                outputManager = new LinuxOutputManager(xCon),
            };
        }

        public ISClientDependencies(IDisposable globalDependency)
        {
            this.globalDependency = globalDependency;
        }

        public ISClientDependencies()
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
                    globalDependency?.Dispose();
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
