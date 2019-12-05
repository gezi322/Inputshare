using InputshareLib.Client;
using InputshareLib.Clipboard;
using InputshareLib.Displays;
using InputshareLib.Server;
using InputshareLibWindows.Clipboard;
using InputshareLibWindows.Displays;
using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.Output;
using InputshareLibWindows.PlatformModules.Clipboard;
using InputshareLibWindows.PlatformModules.Displays;
using InputshareLibWindows.PlatformModules.DragDrop;
using InputshareLibWindows.PlatformModules.Input;
using InputshareLibWindows.PlatformModules.Output;

namespace InputshareLibWindows
{
    public static class WindowsDependencies
    {
        public static ISServerDependencies GetServerDependencies()
        {
            return new ISServerDependencies
            {
                DisplayManager = new WindowsDisplayManager(),
                DragDropManager = new WindowsDragDropManager(),
                InputManager = new WindowsInputManager(),
                OutputManager = new WindowsOutputManager(),
                ClipboardManager = new WindowsClipboardManager()
            };
        }

        public static ISClientDependencies GetClientDependencies()
        {
            return new ISClientDependencies
            {
                clipboardManager = new WindowsClipboardManager(),
                displayManager = new WindowsDisplayManager(),
                dragDropManager = new WindowsDragDropManager(),
                outputManager = new WindowsOutputManager()
            };
        }

        public static ISClientDependencies GetServiceDependencies(IpcHandle mainHost, IpcHandle clipboardHost)
        {
            return new ISClientDependencies
            {
                clipboardManager = new ServiceClipboardManager(clipboardHost),
                displayManager = new ServiceDisplayManager(mainHost),
                dragDropManager = new ServiceDragDropManager(clipboardHost),
                outputManager = new ServiceOutputManager(mainHost)
            };
        }
    }
}
