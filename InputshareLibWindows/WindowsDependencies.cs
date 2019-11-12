using InputshareLib.Client;
using InputshareLib.Clipboard;
using InputshareLib.Cursor;
using InputshareLib.Displays;
using InputshareLib.DragDrop;
using InputshareLib.Server;
using InputshareLibWindows.Clipboard;
using InputshareLibWindows.Cursor;
using InputshareLibWindows.Displays;
using InputshareLibWindows.DragDrop;
using InputshareLibWindows.Input;
using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.Output;

namespace InputshareLibWindows
{
    public static class WindowsDependencies
    {
        public static ISServerDependencies GetServerDependencies()
        {
            return new ISServerDependencies
            {
                DisplayManager = new WindowsDisplayManager(),
                CursorMonitor = new WindowsCursorMonitor(),
                DragDropManager = new WindowsDragDropManager(),
                InputManager = new WindowsInputManager(),
                OutputManager = new WindowsOutputManager(),
                ClipboardManager = new WindowsClipboardManager()
            };
        }

        public static ClientDependencies GetClientDependencies()
        {
            return new ClientDependencies
            {
                clipboardManager = new WindowsClipboardManager(),
                cursorMonitor = new WindowsCursorMonitor(),
                displayManager = new WindowsDisplayManager(),
                dragDropManager = new WindowsDragDropManager(),
                outputManager = new WindowsOutputManager()
            };
        }

        public static ClientDependencies GetServiceDependencies(IpcHandle mainHost, IpcHandle dragDropHost)
        {
            return new ClientDependencies
            {
                clipboardManager = new ServiceClipboardManager(mainHost),
                cursorMonitor = new ServiceCursorMonitor(mainHost),
                displayManager = new ServiceDisplayManager(mainHost),
                dragDropManager = new ServiceDragDropManager(mainHost, dragDropHost),
                outputManager = new ServiceOutputManager(mainHost)
            };
        }
    }
}
