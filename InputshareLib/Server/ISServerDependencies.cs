using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Cursor;
using InputshareLib.PlatformModules.Displays;
using InputshareLib.PlatformModules.DragDrop;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;

namespace InputshareLib.Server
{
    /// <summary>
    /// OS specifid depedencies required to run an inputshare server
    /// </summary>
    public sealed class ISServerDependencies
    {
        public DisplayManagerBase DisplayManager { get; set; }
        public InputManagerBase InputManager { get; set; }
        public CursorMonitorBase CursorMonitor { get; set; }
        public DragDropManagerBase DragDropManager { get; set; }
        public OutputManagerBase OutputManager { get; set; }
        public ClipboardManagerBase ClipboardManager { get; set; }
    }
}
