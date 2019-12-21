using System;
using InputshareLib.Linux;
using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Displays;
using InputshareLib.PlatformModules.DragDrop;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;

namespace InputshareLib.Server
{
    /// <summary>
    /// OS specifid depedencies required to run an inputshare server
    /// </summary>
    public sealed class ISServerDependencies : IDisposable
    {
        private IDisposable globalDependency;

        public DisplayManagerBase DisplayManager { get; set; }
        public InputManagerBase InputManager { get; set; }
        public DragDropManagerBase DragDropManager { get; set; }
        public OutputManagerBase OutputManager { get; set; }
        public ClipboardManagerBase ClipboardManager { get; set; }

        public static ISServerDependencies GetLinuxDependencies()
        {
            var xCon = new SharedXConnection();

            return new ISServerDependencies(xCon)
            {
                ClipboardManager = new LinuxClipboardManager(xCon),
                DisplayManager = new LinuxDisplayManager(xCon),
                DragDropManager = new NullDragDropManager(),
                InputManager = new LinuxInputManager(xCon),
                OutputManager = new LinuxOutputManager(xCon),
            };
        }

        public ISServerDependencies()
        {

        }

        public ISServerDependencies(IDisposable globalDependency)
        {
            this.globalDependency = globalDependency;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ISLogger.Write("Disposing dependency");
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
