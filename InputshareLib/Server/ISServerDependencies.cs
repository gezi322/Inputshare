using InputshareLib.Clipboard;
using InputshareLib.Cursor;
using InputshareLib.Displays;
using InputshareLib.DragDrop;
using InputshareLib.Input;
using InputshareLib.Output;

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
        public IDragDropManager DragDropManager { get; set; }
        public IOutputManager OutputManager { get; set; }
        public ClipboardManagerBase ClipboardManager { get; set; }
    }
}
