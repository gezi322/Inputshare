using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Permissions;
using System.Windows.Forms;

namespace InputshareLibWindows.Clipboard
{
    public class InputshareDataObject : DataObject, System.Runtime.InteropServices.ComTypes.IDataObject, IAsyncOperation
    {
        public event EventHandler DropSuccess;
        public event EventHandler<Guid> DropComplete;

        bool reportedSuccess = false;
        public ClipboardDataType objectType { get; private set; }

        
        private Int32 m_lindex;

        private List<ClipboardVirtualFileData.FileAttributes> operationFiles;
        private MemoryStream fileDescriptorStream;
        private List<ManagedRemoteIStream> streams = new List<ManagedRemoteIStream>();


        public InputshareDataObject(ClipboardTextData text)
        {
            objectType = ClipboardDataType.Text;
            SetData(DataFormats.Text, text.Text);
        }

        public InputshareDataObject(Image image)
        {
            objectType = ClipboardDataType.Image;
            SetImage(image);
        }

        public InputshareDataObject(List<ClipboardVirtualFileData.FileAttributes> files)
        {
            objectType = ClipboardDataType.File;
            fileDescriptorStream = GetFileDescriptor(files);
            foreach (var file in files)
            {
                //ISLogger.Write("Creating remote file stream for " + file.FileName);
                ManagedRemoteIStream str = new ManagedRemoteIStream(file);
                streams.Add(str);
            }

            operationFiles = files;

            byte[] a = BitConverter.GetBytes((int)DragDropEffects.Copy);

            IntPtr p = Marshal.AllocHGlobal(a.Length);
            Marshal.Copy(a, 0, p, a.Length);

            SetData(NativeMethods.CFSTR_FILEDESCRIPTORW, null);
            SetData(NativeMethods.CFSTR_FILECONTENTS, null);
            SetData(NativeMethods.CFSTR_PERFORMEDDROPEFFECT, null);
            SetData(NativeMethods.CFSTR_PREFERREDDROPEFFECT, IntPtr.Zero);
            SetData("InputshareFileData", "Inputshare object");
        }

        public override object GetData(string format, bool autoConvert)
        {
            //Don't tell the shell that we have file contents if we only have text or an image
            if(objectType == ClipboardDataType.File)
            {
                if (String.Compare(format, NativeMethods.CFSTR_FILEDESCRIPTORW, StringComparison.OrdinalIgnoreCase) == 0 && operationFiles != null)
                {
                    base.SetData(NativeMethods.CFSTR_FILEDESCRIPTORW, fileDescriptorStream);
                }
                else if (String.Compare(format, NativeMethods.CFSTR_FILECONTENTS, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    base.SetData(NativeMethods.CFSTR_FILECONTENTS, GetFileContents(m_lindex));
                }
                else if (String.Compare(format, NativeMethods.CFSTR_PERFORMEDDROPEFFECT, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    base.SetData(NativeMethods.CFSTR_PREFERREDDROPEFFECT, DragDropEffects.All);
                }

            }
            return base.GetData(format, autoConvert);
        }

        void System.Runtime.InteropServices.ComTypes.IDataObject.SetData(ref FORMATETC format, ref STGMEDIUM medium, bool release)
        {

        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        void System.Runtime.InteropServices.ComTypes.IDataObject.GetData(ref System.Runtime.InteropServices.ComTypes.FORMATETC formatetc, out System.Runtime.InteropServices.ComTypes.STGMEDIUM medium)
        {
            try
            {
                medium = new System.Runtime.InteropServices.ComTypes.STGMEDIUM();
                
                if (formatetc.cfFormat == (Int16)DataFormats.GetFormat(NativeMethods.CFSTR_FILECONTENTS).Id)
                    m_lindex = formatetc.lindex;
               
                if (GetTymedUseable(formatetc.tymed))
                {
                    if ((formatetc.tymed & TYMED.TYMED_ISTREAM) != TYMED.TYMED_NULL)
                    {
                        if (objectType != ClipboardDataType.File)
                            return;
                            
                        try
                        {
                            //DropSuccess?.Invoke(this, null);
                            medium.tymed = TYMED.TYMED_ISTREAM;
                            IStream o = (IStream)GetData("FileContents", false);
                            medium.unionmember = Marshal.GetComInterfaceForObject(o, typeof(IStream));
                            return;
                        }
                        catch (Exception)
                        {
                            //ISLogger.Write("InputshareDataObject: Get FileContents failed: " + ex.Message);
                            return;
                        }
                    }
                    else if ((formatetc.tymed & TYMED.TYMED_HGLOBAL) != TYMED.TYMED_NULL)
                    {
                        medium.tymed = TYMED.TYMED_HGLOBAL;
                        medium.unionmember = NativeMethods.GlobalAlloc(NativeMethods.GHND | NativeMethods.GMEM_DDESHARE, 1);
                        if (medium.unionmember == IntPtr.Zero)
                        {
                            throw new OutOfMemoryException();
                        }
                        try
                        {

                            ((System.Runtime.InteropServices.ComTypes.IDataObject)this).GetDataHere(ref formatetc, ref medium);
                            return;
                        }
                        catch
                        {
                            NativeMethods.GlobalFree(new HandleRef((STGMEDIUM)medium, medium.unionmember));
                            medium.unionmember = IntPtr.Zero;
                            return;
                        }
                    }
                    medium.tymed = formatetc.tymed;
                    ((System.Runtime.InteropServices.ComTypes.IDataObject)this).GetDataHere(ref formatetc, ref medium);
                }
                else
                {
                    Marshal.ThrowExceptionForHR(NativeMethods.DV_E_TYMED);
                }
            }catch(Exception ex)
            {
                medium = new STGMEDIUM();
                ISLogger.Write("InputshareDataObject: " + ex.Message);
            }
            
        }

        private static Boolean GetTymedUseable(TYMED tymed)
        {
            for (Int32 i = 0; i < ALLOWED_TYMEDS.Length; i++)
            {
                if ((tymed & ALLOWED_TYMEDS[i]) != TYMED.TYMED_NULL)
                {
                    return true;
                }
            }
            return false;
        }

        private MemoryStream GetFileDescriptor(List<ClipboardVirtualFileData.FileAttributes> files)
        {
            try
            {
                MemoryStream FileDescriptorMemoryStream = new MemoryStream();
                // Write out the FILEGROUPDESCRIPTOR.cItems value
                FileDescriptorMemoryStream.Write(BitConverter.GetBytes(files.Count), 0, sizeof(UInt32));

                FILEDESCRIPTOR FileDescriptor = new FILEDESCRIPTOR();
                foreach (var si in files)
                {
                    string n = si.RelativePath;

                    //If the file is in the root folder of the operation, the relative path will not be set!
                    if (string.IsNullOrEmpty(si.RelativePath))
                    {
                        n = si.FileName;
                    }

                    FileDescriptor.cFileName = n;
                    Int64 FileWriteTimeUtc = si.LastChangeTime.ToFileTimeUtc();
                    FileDescriptor.ftLastWriteTime.dwHighDateTime = (Int32)(FileWriteTimeUtc >> 32);
                    FileDescriptor.ftLastWriteTime.dwLowDateTime = (Int32)(FileWriteTimeUtc & 0xFFFFFFFF);
                    FileDescriptor.nFileSizeHigh = (UInt32)(si.FileSize >> 32);
                    FileDescriptor.nFileSizeLow = (UInt32)(si.FileSize & 0xFFFFFFFF);
                    FileDescriptor.dwFlags = NativeMethods.FD_WRITESTIME | NativeMethods.FD_FILESIZE | NativeMethods.FD_PROGRESSUI;

                    Int32 FileDescriptorSize = Marshal.SizeOf(FileDescriptor);
                    IntPtr FileDescriptorPointer = Marshal.AllocHGlobal(FileDescriptorSize);
                    Marshal.StructureToPtr(FileDescriptor, FileDescriptorPointer, true);
                    Byte[] FileDescriptorByteArray = new Byte[FileDescriptorSize];
                    Marshal.Copy(FileDescriptorPointer, FileDescriptorByteArray, 0, FileDescriptorSize);
                    Marshal.FreeHGlobal(FileDescriptorPointer);
                    FileDescriptorMemoryStream.Write(FileDescriptorByteArray, 0, FileDescriptorByteArray.Length);
                }
                return FileDescriptorMemoryStream;
            }catch(Exception ex)
            {
                ISLogger.Write("Get file descriptor failed: " + ex.Message);
                return null;
            }
        }

        private IStream GetFileContents(Int32 FileNumber)
        {
            if (FileNumber == -1)
            {
                return null;
            }

            return streams[FileNumber];
        }

        private bool inOperation = false;
        private bool completeSent = false;
        public void SetAsyncMode(int fDoOpAsync)
        {

        }

        public void GetAsyncMode(out int pfIsOpAsync)
        {
            pfIsOpAsync = NativeMethods.VARIANT_TRUE;
        }

        public void StartOperation(IBindCtx pbcReserved)
        {
            inOperation = true;

            if (!reportedSuccess)
            {
                reportedSuccess = true;
                DropSuccess?.Invoke(this, null);
            }

        }

        public void InOperation(out int pfInAsyncOp)
        {
            pfInAsyncOp = inOperation ? NativeMethods.VARIANT_TRUE : NativeMethods.VARIANT_FALSE;
        }

        public void EndOperation(int hResult, IBindCtx pbcReserved, uint dwEffects)
        {
            inOperation = false;
            if (!completeSent)
            {
                completeSent = true;
                DropComplete?.Invoke(this, operationFiles[0].FileOperationId);
            }
        }

        private static readonly TYMED[] ALLOWED_TYMEDS =
            new TYMED[] {
                TYMED.TYMED_HGLOBAL,
                TYMED.TYMED_ISTREAM,
                TYMED.TYMED_ENHMF,
                TYMED.TYMED_MFPICT,
                TYMED.TYMED_GDI};

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct FILEDESCRIPTOR
        {
            public UInt32 dwFlags;
            public Guid clsid;
            public System.Drawing.Size sizel;
            public System.Drawing.Point pointl;
            public UInt32 dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public UInt32 nFileSizeHigh;
            public UInt32 nFileSizeLow;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public String cFileName;
        }

    }

    public partial class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GlobalAlloc(int uFlags, int dwBytes);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GlobalFree(HandleRef handle);

        public const int VARIANT_FALSE = 0;
        public const int VARIANT_TRUE = -1; 

        public const string CFSTR_PREFERREDDROPEFFECT = "Preferred DropEffect";
        public const string CFSTR_PERFORMEDDROPEFFECT = "Performed DropEffect";
        public const string CFSTR_FILEDESCRIPTORW = "FileGroupDescriptorW";
        public const string CFSTR_FILECONTENTS = "FileContents";

        public const Int32 FD_CLSID = 0x00000001;
        public const Int32 FD_SIZEPOINT = 0x00000002;
        public const Int32 FD_ATTRIBUTES = 0x00000004;
        public const Int32 FD_CREATETIME = 0x00000008;
        public const Int32 FD_ACCESSTIME = 0x00000010;
        public const Int32 FD_WRITESTIME = 0x00000020;
        public const Int32 FD_FILESIZE = 0x00000040;
        public const Int32 FD_PROGRESSUI = 0x00004000;
        public const Int32 FD_LINKUI = 0x00008000;

        public const Int32 GMEM_MOVEABLE = 0x0002;
        public const Int32 GMEM_ZEROINIT = 0x0040;
        public const Int32 GHND = (GMEM_MOVEABLE | GMEM_ZEROINIT);
        public const Int32 GMEM_DDESHARE = 0x2000;

        public const Int32 DV_E_TYMED = unchecked((Int32)0x80040069);


    }


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
}
