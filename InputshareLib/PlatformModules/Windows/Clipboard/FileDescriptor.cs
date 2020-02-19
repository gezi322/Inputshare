using InputshareLib.Net.RFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using static InputshareLib.PlatformModules.Windows.Native.Kernel32;
using System.Text;
using System.ComponentModel;

namespace InputshareLib.PlatformModules.Windows.Clipboard
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct FILEDESCRIPTOR
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

        private const int FD_CLSID = 0x00000001;
        private const int FD_SIZEPOINT = 0x00000002;
        private const int FD_ATTRIBUTES = 0x00000004;
        private const int FD_CREATETIME = 0x00000008;
        private const int FD_ACCESSTIME = 0x00000010;
        private const int FD_WRITESTIME = 0x00000020;
        private const int FD_FILESIZE = 0x00000040;
        private const int FD_PROGRESSUI = 0x00004000;
        private const int FD_LINKUI = 0x00008000;


        internal static MemoryStream GenerateFileDescriptor(RFSFileGroup files)
        {
            try
            {
                MemoryStream FileDescriptorMemoryStream = new MemoryStream();
                // Write out the FILEGROUPDESCRIPTOR.cItems value
                FileDescriptorMemoryStream.Write(BitConverter.GetBytes(files.Files.Length), 0, sizeof(UInt32));

                FILEDESCRIPTOR FileDescriptor = new FILEDESCRIPTOR();
                foreach (var si in files.Files)
                {
                    FileDescriptor.cFileName = si.RelativePath;
                    Int64 FileWriteTimeUtc = 0;
                    FileDescriptor.ftLastWriteTime.dwHighDateTime = (Int32)(FileWriteTimeUtc >> 32);
                    FileDescriptor.ftLastWriteTime.dwLowDateTime = (Int32)(FileWriteTimeUtc & 0xFFFFFFFF);
                    FileDescriptor.nFileSizeHigh = (UInt32)(si.FileSize >> 32);
                    FileDescriptor.nFileSizeLow = (UInt32)(si.FileSize & 0xFFFFFFFF);
                    FileDescriptor.dwFlags = FD_WRITESTIME | FD_FILESIZE | FD_PROGRESSUI;

                    Int32 FileDescriptorSize = Marshal.SizeOf(FileDescriptor);
                    IntPtr FileDescriptorPointer = Marshal.AllocHGlobal(FileDescriptorSize);
                    Marshal.StructureToPtr(FileDescriptor, FileDescriptorPointer, true);
                    Byte[] FileDescriptorByteArray = new Byte[FileDescriptorSize];
                    Marshal.Copy(FileDescriptorPointer, FileDescriptorByteArray, 0, FileDescriptorSize);
                    Marshal.FreeHGlobal(FileDescriptorPointer);
                    FileDescriptorMemoryStream.Write(FileDescriptorByteArray, 0, FileDescriptorByteArray.Length);
                }
                return FileDescriptorMemoryStream;
            }
            catch (Exception ex)
            {
                Logger.Write("Get file descriptor failed: " + ex.Message);
                return null;
            }
        }
    }
}
