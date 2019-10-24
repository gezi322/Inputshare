using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace InputshareLibWindows.Clipboard
{
    /// <summary>
    /// Enables transfering data from an inputshare server/client to windows clipboard
    /// </summary>
    internal class ManagedRemoteIStream : IStream
    {
        /// <summary>
        /// The file that is being written to the stream
        /// </summary>
        public ClipboardVirtualFileData.FileAttributes SourceVirtualFile;

        /// <summary>
        /// If true, the read method returns -1 back to the shell
        /// </summary>
        private bool closeStream = false;

        /// <summary>
        /// Creates an IStream that uses a network machine as the source of data
        /// </summary>
        /// <param name="file"></param>
        internal ManagedRemoteIStream(ClipboardVirtualFileData.FileAttributes file)
        {
            SourceVirtualFile = file;
            SourceVirtualFile.CloseStreamRequested += FileInfo_CloseStreamRequested;
        }

        private void FileInfo_CloseStreamRequested(object sender, EventArgs e)
        {
            closeStream = true;
        }

        /// <summary>
        /// The ammount of data that has been read from the source stream.
        /// We use this to check if the file is fully read
        /// 
        /// The method calls the SourceFileInfo.FileReadDelgate to fetch data from the host with the
        /// file access token specified in the SourceFileInfo.
        /// </summary>
        long readLen = 0;
        void IStream.Read(Byte[] buffer, Int32 bufferSize, IntPtr bytesReadPtr)
        {

            try
            {
                //Check that close has not been called
                if (closeStream)
                {
                    Marshal.WriteInt32(bytesReadPtr, -1);
                    return;
                }

                //TODO - shell reads 16 bytes of data and discards it when the drop begins??
                if(bufferSize == 16)
                {
                    Marshal.WriteInt32(bytesReadPtr, -1);
                    return;
                }

                //check if file is fully read
                if(readLen >= SourceVirtualFile.FileSize)
                {
                    Marshal.WriteInt32(bytesReadPtr, 0);
                    OnComplete();
                    return;
                }
                
                //using await will break the dragdrop operation!
                byte[] data = SourceVirtualFile.ReadDelegate(SourceVirtualFile.RemoteAccessToken, SourceVirtualFile.FileOperationId, SourceVirtualFile.FileRequestId, bufferSize).Result;

                //check that close has not been called
                if (closeStream || data.Length == 0)
                {
                    Marshal.WriteInt32(bytesReadPtr, -1);
                    return;
                }

                //copy the data to the buffer, and write the length of the data to BytesReadPtr
                Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
                Marshal.WriteInt32(bytesReadPtr, data.Length);
                readLen += data.Length;
            }
            catch (Exception ex)
            {
                ISLogger.Write("ManagedRemoteIStream: Failed to read remote file: " + ex.Message);
            }
        }

        void OnComplete()
        {
            SourceVirtualFile.MarkComplete();
            return;
        }

        void IStream.Seek(Int64 offset, Int32 origin, IntPtr newPositionPtr)
        {
            Marshal.WriteInt64(newPositionPtr, -1);
            return;
            //Todo - disabling seeking seems to work fine?
        }

        #region unimplemented
        void IStream.SetSize(Int64 libNewSize)
        {

        }
        void IStream.Stat(out System.Runtime.InteropServices.ComTypes.STATSTG streamStats, int grfStatFlag)
        {
            streamStats = new STATSTG();
            ISLogger.Write("STAT CALLED");

        }
        void IStream.Write(Byte[] buffer, Int32 bufferSize, IntPtr bytesWrittenPtr)
        {

        }
        void IStream.Clone(out IStream streamCopy)
        {
            streamCopy = null;
            throw new NotSupportedException();
        }

        void IStream.CopyTo(IStream targetStream, Int64 bufferSize, IntPtr buffer, IntPtr bytesWrittenPtr)
        {
            throw new NotSupportedException();
        }
        void IStream.Commit(Int32 flags)
        {
            throw new NotSupportedException();
        }
        void IStream.LockRegion(Int64 offset, Int64 byteCount, Int32 lockType)
        {
            throw new NotSupportedException();
        }
        void IStream.Revert()
        {
            throw new NotSupportedException();
        }
        void IStream.UnlockRegion(Int64 offset, Int64 byteCount, Int32 lockType)
        {
            throw new NotSupportedException();
        }
        #endregion


        #region native
        internal const int STGM_READ = 0x00000000;
        internal const int STGM_WRITE = 0x00000001;

        internal const int STGM_READWRITE = 0x00000002;

        internal const int STGM_READWRITE_Bits = 0x00000003; // Not a STGM enumeration, used to strip out all STGM bits not relating to read/write access
        //  Sharing 
        internal const int STGM_SHARE_DENY_NONE = 0x00000040;
        internal const int STGM_SHARE_DENY_READ = 0x00000030;
        internal const int STGM_SHARE_DENY_WRITE = 0x00000020;
        internal const int STGM_SHARE_EXCLUSIVE = 0x00000010;
        internal const int STGM_PRIORITY = 0x00040000; // Not Currently Supported
        //  Creation 
        internal const int STGM_CREATE = 0x00001000;
        internal const int STGM_CONVERT = 0x00020000;
        internal const int STGM_FAILIFTHERE = 0x00000000;
        //  Transactioning 
        internal const int STGM_DIRECT = 0x00000000; // Not Currently Supported 
        internal const int STGM_TRANSACTED = 0x00010000; // Not Currently Supported 
        //  Transactioning Performance 
        internal const int STGM_NOSCRATCH = 0x00100000; // Not Currently Supported
        internal const int STGM_NOSNAPSHOT = 0x00200000; // Not Currently Supported
        //  Direct SWMR and Simple 
        internal const int STGM_SIMPLE = 0x08000000; // Not Currently Supported
        internal const int STGM_DIRECT_SWMR = 0x00400000; // Not Currently Supported
        //  Delete On Release 
        internal const int STGM_DELETEONRELEASE = 0x04000000; // Not Currently Supported

        internal const int STREAM_SEEK_SET = 0;
        internal const int STREAM_SEEK_CUR = 1;
        internal const int STREAM_SEEK_END = 2;

        internal const int STATFLAG_NONAME = 1;
        internal const int STATFLAG_NOOPEN = 2;

        internal const int STGTY_STORAGE = 1;
        internal const int STGTY_STREAM = 2;
        internal const int STGTY_LOCKBYTES = 3;
        internal const int STGTY_PROPERTY = 4;

        internal const uint PROPSETFLAG_ANSI = 2;

        internal const int S_OK = 0;
        internal const int S_FALSE = 1;
        internal const int STG_E_FILENOTFOUND = -2147287038; //0x80030002;
        internal const int STG_E_ACCESSDENIED = -2147287035; //0x80030005;
        internal const int STG_E_FILEALREADYEXISTS = -2147286960; //0x80030050;
        internal const int STG_E_INVALIDNAME = -2147286788; //0x800300FC;
        internal const int STG_E_INVALIDFLAG = -2147286785; //0x800300FF;
        #endregion
    }
}
