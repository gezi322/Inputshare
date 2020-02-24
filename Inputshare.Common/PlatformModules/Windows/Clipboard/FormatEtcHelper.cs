using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Inputshare.Common.PlatformModules.Windows.Clipboard
{
    internal static class FormatEtcHelper
    {
        internal static FORMATETC CreateTempFormat()
        {
            return new FORMATETC
            {
                cfFormat = (short)WinClipboardDataFormat.InputshareFormat,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_NULL
            };
        }

        internal static FORMATETC CreateUnicodeFormat()
        {
            return new FORMATETC
            {
                cfFormat = (short)WinClipboardDataFormat.CF_UNICODETEXT,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_HGLOBAL
            };
        }

        internal static FORMATETC CreateFileContentsFormat()
        {
            return new FORMATETC
            {
                cfFormat = (short)WinClipboardDataFormat.CFSTR_FILECONTENTS,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_ISTREAM
            };
        }

        internal static FORMATETC CreateFileDescriptorWFormat()
        {
            return new FORMATETC
            {
                cfFormat = (short)WinClipboardDataFormat.CFSTR_FILEDESCRIPTOR,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_HGLOBAL
            };
        }

        internal static FORMATETC CreatePreferredEffectFormat()
        {
            return new FORMATETC
            {
                cfFormat = WinClipboardDataFormat.PREFERREDDROPEFFECT,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_HGLOBAL
            };
        }
        internal static FORMATETC CreateBitmapFormat()
        {
            return new FORMATETC
            {
                cfFormat = (short)WinClipboardDataFormat.CF_BITMAP,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_GDI
            };
        }
    }
}
