﻿using InputshareLib.Linux;
using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Displays;
using InputshareLib.PlatformModules.DragDrop;
using InputshareLib.PlatformModules.Output;

namespace InputshareLib.Client
{
    public class ISClientDependencies
    {
        public OutputManagerBase outputManager { get; set; }
        public ClipboardManagerBase clipboardManager { get; set; }
        public DisplayManagerBase displayManager { get; set; }
        public DragDropManagerBase dragDropManager { get; set; }

        public static ISClientDependencies GetLinuxDependencies()
        {
            Linux.SharedXConnection xCon = new SharedXConnection();

            return new ISClientDependencies()
            {
                clipboardManager = new LinuxClipboardManager(xCon),
                displayManager = new LinuxDisplayManager(xCon),
                dragDropManager = new NullDragDropManager(),
                outputManager = new LinuxOutputManager(xCon),
            };
        }
    }
}