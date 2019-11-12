using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Cursor;
using InputshareLib.PlatformModules.Displays;
using InputshareLib.PlatformModules.DragDrop;
using InputshareLib.PlatformModules.Output;

namespace InputshareLib.Client
{
    public class ClientDependencies
    {
        public OutputManagerBase outputManager { get; set; }
        public ClipboardManagerBase clipboardManager { get; set; }
        public CursorMonitorBase cursorMonitor { get; set; }
        public DisplayManagerBase displayManager { get; set; }
        public DragDropManagerBase dragDropManager { get; set; }
    }
}
