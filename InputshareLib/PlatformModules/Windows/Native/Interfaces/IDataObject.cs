using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace InputshareLib.PlatformModules.Windows.Native.Interfaces
{

    [ComImport, Guid("0000010E-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDataObject
    {
        [PreserveSig]
        IntPtr GetData([In] ref FORMATETC format, out STGMEDIUM medium);
        [PreserveSig]
        IntPtr GetDataHere([In] ref FORMATETC format, ref STGMEDIUM medium);
        [PreserveSig]
        IntPtr QueryGetData([In] ref FORMATETC format);
        [PreserveSig]
        IntPtr GetCanonicalFormatEtc([In] ref FORMATETC formatIn, out FORMATETC formatOut);
        [PreserveSig]
        IntPtr SetData([In] ref FORMATETC formatIn, [In] ref STGMEDIUM medium, [MarshalAs(UnmanagedType.Bool)] bool release);
        [PreserveSig]
        IntPtr EnumFormatEtc(DATADIR direction, out System.Runtime.InteropServices.ComTypes.IEnumFORMATETC ppenumFormatEtc);
        [PreserveSig]
        IntPtr DAdvise([In] ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection);
        [PreserveSig]
        IntPtr DUnadvise(int connection);
        [PreserveSig]
        IntPtr EnumDAdvise(out IEnumSTATDATA enumAdvise);
    }

}
