using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using static InputshareLibWindows.Native.Ole32;

namespace InputshareLibWindows.Clipboard
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

        public static MemoryStream GenerateFileDescriptor(List<InputshareLib.Clipboard.DataTypes.FileAttributes> files)
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
                ISLogger.Write("Get file descriptor failed: " + ex.Message);
                return null;
            }
        }
    }
}
