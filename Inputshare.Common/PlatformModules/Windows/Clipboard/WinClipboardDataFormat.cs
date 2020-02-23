using System;
using System.Collections.Generic;
using System.Text;
using static Inputshare.Common.PlatformModules.Windows.Native.User32;

namespace Inputshare.Common.PlatformModules.Windows.Clipboard
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



        /// <summary>
        /// Gets the name of the given format ID
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GetFormatName(uint format)
        {
            switch (format)
            {
                case 1:
                    return "CF_TEXT";
                case 2:
                    return "CF_BITMAP";
                case 3:
                    return "CF_METAFILEPICT";
                case 5:
                    return "CF_DIB";
                case 7:
                    return "CF_OEMTEXT";
                case 8:
                    return "CF_DIB";
                case 17:
                    return "CF_DIBV5";
                case 13:
                    return "CF_UNICODETEXT";
                case 15:
                    return "CF_HDROP";
                case 16:
                    return "CF_LOCALE";
                case 0x0082:
                    return "CF_DSPBITMAP";
                case 0x008E:
                    return "CF_DSPENHMETAFILE";
                case 0x0083:
                    return "CF_DSPMETAFILEPICT";
                case 0x0081:
                    return "CF_DSPTEXT";
                case 14:
                    return "CF_ENHMETAFILE";
                case 0x0300:
                    return "CF_GDIOBJFIRST";
                case 0x03FF:
                    return "CF_GDIOBJLAST";
                case 0x0089:
                    return "CF_OWNERDISPLAY";
                case 9:
                    return "CF_PALETTE";
                case 10:
                    return "CF_PENDATA";
                case 0x0200:
                    return "CF_PRIVATEFIRST";
                case 0x02FF:
                    return "CF_PRIVATELAST";
                case 11:
                    return "CF_RIFF";
                case 4:
                    return "CF_SYLK";
                case 6:
                    return "CF_TIFF";
                case 12:
                    return "CF_WAVE";
                default:
                    {
                        StringBuilder sb = new StringBuilder(64);
                        GetClipboardFormatName(format, sb, 64);

                        if (string.IsNullOrWhiteSpace(sb.ToString()))
                            return format.ToString();

                        return sb.ToString();
                    }

            }
        }
    }
}
