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
        int GetData([In] ref FORMATETC format, out STGMEDIUM medium);
        [PreserveSig]
        int GetDataHere([In] ref FORMATETC format, ref STGMEDIUM medium);
        [PreserveSig]
        int QueryGetData([In] ref FORMATETC format);
        [PreserveSig]
        int GetCanonicalFormatEtc([In] ref FORMATETC formatIn, out FORMATETC formatOut);
        [PreserveSig]
        int SetData([In] ref FORMATETC formatIn, [In] ref STGMEDIUM medium, [MarshalAs(UnmanagedType.Bool)] bool release);
        [PreserveSig]
        int EnumFormatEtc(DATADIR direction, out System.Runtime.InteropServices.ComTypes.IEnumFORMATETC ppenumFormatEtc);
        [PreserveSig]
        int DAdvise([In] ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection);
        [PreserveSig]
        int DUnadvise(int connection);
        [PreserveSig]
        int EnumDAdvise(out IEnumSTATDATA enumAdvise);
    }

}
