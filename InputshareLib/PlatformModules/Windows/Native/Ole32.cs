using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using IDataObject = Inputshare.Common.PlatformModules.Windows.Native.Interfaces.IDataObject;

namespace Inputshare.Common.PlatformModules.Windows.Native
{
    internal static class Ole32
    {
        [DllImport("shell32.dll")]
        internal static extern IntPtr SHCreateStdEnumFmtEtc(uint format, FORMATETC[] formats, out IEnumFORMATETC etc);

           [DllImport("ole32.dll", SetLastError = true)]
        internal static extern IntPtr OleFlushClipboard();

        [DllImport("ole32.dll", SetLastError = true)]
        internal static extern IntPtr OleSetClipboard([In]IDataObject pDataObj);

        [DllImport("ole32.dll")]
        internal static extern IntPtr OleGetClipboard([MarshalAs(UnmanagedType.Interface)]out IDataObject ppDataObj);

        [DllImport("ole32.dll", PreserveSig = false)]
        internal static extern void OleInitialize(IntPtr pvReserved);
    }
}
