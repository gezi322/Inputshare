﻿using InputshareLib.Client;
using InputshareLib.Clipboard;
using InputshareLib.Displays;
using InputshareLib.Server;
using InputshareLibWindows.Clipboard;
using InputshareLibWindows.Cursor;
using InputshareLibWindows.Displays;
using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.Output;
using InputshareLibWindows.PlatformModules.Clipboard;
using InputshareLibWindows.PlatformModules.Cursor;
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
                CursorMonitor = new WindowsCursorMonitor(),
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
                cursorMonitor = new WindowsCursorMonitor(),
                displayManager = new WindowsDisplayManager(),
                dragDropManager = new WindowsDragDropManager(),
                outputManager = new WindowsOutputManager()
            };
        }

        public static ISClientDependencies GetServiceDependencies(IpcHandle mainHost, IpcHandle dragDropHost)
        {
            return new ISClientDependencies
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
