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
    public sealed class ISServerDependencies
    {
        public DisplayManagerBase DisplayManager { get; set; }
        public InputManagerBase InputManager { get; set; }
        public DragDropManagerBase DragDropManager { get; set; }
        public OutputManagerBase OutputManager { get; set; }
        public ClipboardManagerBase ClipboardManager { get; set; }

        public static ISServerDependencies GetLinuxDependencies()
        {
            SharedXConnection xCon = new SharedXConnection();

            return new ISServerDependencies()
            {
                ClipboardManager = new LinuxClipboardManager(xCon),
                DisplayManager = new LinuxDisplayManager(xCon),
                DragDropManager = new NullDragDropManager(),
                InputManager = new LinuxInputManager(xCon),
                OutputManager = new LinuxOutputManager(xCon),
            };
        }
    }
}
