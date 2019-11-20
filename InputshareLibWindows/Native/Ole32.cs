using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace InputshareLibWindows.Native
{
    public static class Ole32
    {
        [ComImport]
        [Guid("3D8B0590-F691-11d2-8EA9-006097DF5BD4")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAsyncOperation
        {
            void SetAsyncMode([In] Int32 fDoOpAsync);
            void GetAsyncMode([Out] out Int32 pfIsOpAsync);
            void StartOperation([In] IBindCtx pbcReserved);
            void InOperation([Out] out Int32 pfInAsyncOp);
            void EndOperation([In] Int32 hResult, [In] IBindCtx pbcReserved, [In] UInt32 dwEffects);
        }

        public const int FD_CLSID = 0x00000001;
        public const int FD_SIZEPOINT = 0x00000002;
        public const int FD_ATTRIBUTES = 0x00000004;
        public const int FD_CREATETIME = 0x00000008;
        public const int FD_ACCESSTIME = 0x00000010;
        public const int FD_WRITESTIME = 0x00000020;
        public const int FD_FILESIZE = 0x00000040;
        public const int FD_PROGRESSUI = 0x00004000;
        public const int FD_LINKUI = 0x00008000;



        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#", Justification = "Win32 API.")]
        [DllImport("shell32.dll")]
        public static extern int SHCreateStdEnumFmtEtc(uint cfmt, FORMATETC[] afmt, out IEnumFORMATETC ppenumFormatEtc);

        [ComImport]
        [Guid("00000121-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDropSource
        {
            [PreserveSig]
            int QueryContinueDrag(int fEscapePressed, uint grfKeyState);
            [PreserveSig]
            int GiveFeedback(uint dwEffect);
        }

        public const int DRAGDROP_S_DROP = 0x00040100;
        public const int DRAGDROP_S_CANCEL = 0x00040101;
        public const int DRAGDROP_S_USEDEFAULTCURSORS = 0x00040102;
        public const int S_OK = 0;
        public const int S_FALSE = 1;

        [DllImport("ole32.dll", CharSet = CharSet.Auto, ExactSpelling = true, PreserveSig = false)]
        public static extern void DoDragDrop(System.Runtime.InteropServices.ComTypes.IDataObject dataObject, IDropSource dropSource, int allowedEffects, int[] finalEffect);

        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint DateTimeLow;
            public uint DateTimeHigh;
        }

        public struct STATSTG
        {
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pwcsName;
            public int type;
            public long cbSize;
            public FILETIME mtime;
            public FILETIME ctime;
            public FILETIME atime;
            public int grfMode;
            public int grfLocksSupported;
            public Guid clsid;
            public int grfStateBits;
            public int reserved;
        }

        [ComImport]
        [Guid("0000000d-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumSTATSTG
        {
            // The user needs to allocate an STATSTG array whose size is celt.
            [PreserveSig]
            uint
            Next(
            uint celt,
            [MarshalAs(UnmanagedType.LPArray), Out]
    STATSTG[] rgelt,
            out uint pceltFetched
            );

            void Skip(uint celt);

            void Reset();

            [return: MarshalAs(UnmanagedType.Interface)]
            IEnumSTATSTG Clone();
        }

        [ComImport]
        [Guid("0000000b-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IStorage
        {
            void CreateStream(
                /* [string][in] */ string pwcsName,
                /* [in] */ uint grfMode,
                /* [in] */ uint reserved1,
                /* [in] */ uint reserved2,
                /* [out] */ out IStream ppstm);

            void OpenStream(
                /* [string][in] */ string pwcsName,
                /* [unique][in] */ IntPtr reserved1,
                /* [in] */ uint grfMode,
                /* [in] */ uint reserved2,
                /* [out] */ out IStream ppstm);

            void CreateStorage(
                /* [string][in] */ string pwcsName,
                /* [in] */ uint grfMode,
                /* [in] */ uint reserved1,
                /* [in] */ uint reserved2,
                /* [out] */ out IStorage ppstg);

            void OpenStorage(
                /* [string][unique][in] */ string pwcsName,
                /* [unique][in] */ IStorage pstgPriority,
                /* [in] */ uint grfMode,
                /* [unique][in] */ IntPtr snbExclude,
                /* [in] */ uint reserved,
                /* [out] */ out IStorage ppstg);

            void CopyTo(
                /* [in] */ uint ciidExclude,
                /* [size_is][unique][in] */ [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] Guid[] rgiidExclude,
                /* [unique][in] */ IntPtr snbExclude,
                /* [unique][in] */ IStorage pstgDest);

            void MoveElementTo(
                /* [string][in] */ string pwcsName,
                /* [unique][in] */ IStorage pstgDest,
                /* [string][in] */ string pwcsNewName,
                /* [in] */ uint grfFlags);

            void Commit(
                /* [in] */ uint grfCommitFlags);

            void Revert();

            void EnumElements(
                /* [in] */ uint reserved1,
                /* [size_is][unique][in] */ IntPtr reserved2,
                /* [in] */ uint reserved3,
                /* [out] */ out IEnumSTATSTG ppenum);

            void DestroyElement(
                /* [string][in] */ string pwcsName);

            void RenameElement(
                /* [string][in] */ string pwcsOldName,
                /* [string][in] */ string pwcsNewName);

            void SetElementTimes(
                /* [string][unique][in] */ string pwcsName,
                /* [unique][in] */ FILETIME pctime,
                /* [unique][in] */ FILETIME patime,
                /* [unique][in] */ FILETIME pmtime);

            void SetClass(
                /* [in] */ Guid clsid);

            void SetStateBits(
                /* [in] */ uint grfStateBits,
                /* [in] */ uint grfMask);

            void Stat(
                /* [out] */ out STATSTG pstatstg,
                /* [in] */ uint grfStatFlag);

        }
    }
}
