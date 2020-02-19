using System;
using System.Collections.Generic;
using System.Text;
using static InputshareLib.PlatformModules.Windows.Native.User32;

namespace InputshareLib.PlatformModules.Windows.Clipboard
{
    public static class WinClipboardDataFormat
    {
        public const uint CF_TEXT = 1;
        public const uint CF_BITMAP = 2;
        public const uint CF_HDROP = 15;
        public const uint CF_UNICODETEXT = 13;
        public static short CFSTR_FILEDESCRIPTOR => (short)RegisterClipboardFormat("FileGroupDescriptorW");
        public static short CFSTR_FILECONTENTS => (short)RegisterClipboardFormat("FileContents");
        public static short InputshareFormat => (short)RegisterClipboardFormat("InputshareTempData");
        public static short PREFERREDDROPEFFECT => (short)RegisterClipboardFormat("Preferred DropEffect");
    }
}
