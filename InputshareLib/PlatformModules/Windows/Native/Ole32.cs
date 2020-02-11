using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace InputshareLib.PlatformModules.Windows.Native
{
    internal static class Ole32
    {
        [DllImport("ole32.dll")]
        internal static extern int OleGetClipboard([MarshalAs(UnmanagedType.IUnknown)]out IDataObject ppDataObj);

        [DllImport("ole32.dll", PreserveSig = false)]
        internal static extern void OleInitialize(IntPtr pvReserved);
    }
}
