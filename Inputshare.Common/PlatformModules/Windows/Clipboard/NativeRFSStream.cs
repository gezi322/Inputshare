﻿using Inputshare.Common.Net.RFS.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Inputshare.Common.PlatformModules.Windows.Clipboard
{
    /// <summary>
    /// Allows sharing a RFS stream on the windows clipboard as a standard IStream
    /// </summary>
    internal class NativeRFSStream : IStream
    {
        private RFSClientStream _stream;

        internal NativeRFSStream(RFSClientStream original)
        {
            _stream = original;
        }

        public void Clone(out IStream ppstm)
        {
            ppstm = null;
        }

        public void Commit(int grfCommitFlags)
        {

        }

        public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {

        }

        public void LockRegion(long libOffset, long cb, int dwLockType)
        {

        }

        public void Read(byte[] pv, int cb, IntPtr pcbRead)
        {
            try
            {
                byte[] data = new byte[cb];
                int bIn = _stream.Read(data, 0, cb);
                Buffer.BlockCopy(data, 0, pv, 0, data.Length);
                Marshal.WriteIntPtr(pcbRead, new IntPtr(bIn));
            }catch(Exception ex)
            {
                Logger.Write("NativeRFSStream read failed: " + ex.Message);
                Marshal.WriteIntPtr(pcbRead, new IntPtr(0));
            }
            
        }

        public void Revert()
        {

        }

        public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
        {
            try
            {
                long val = _stream.Seek(dlibMove, (SeekOrigin)dwOrigin);
                Marshal.WriteIntPtr(plibNewPosition, new IntPtr(val));
            }
            catch (Exception ex)
            {
                Logger.Write("NativeRFSStream seek failed: " + ex.Message);
                Marshal.WriteIntPtr(plibNewPosition, new IntPtr(-1));
            }
            
        }

        public void SetSize(long libNewSize)
        {

        }

        public void Stat(out STATSTG pstatstg, int grfStatFlag)
        {
            pstatstg = new STATSTG();
        }

        public void UnlockRegion(long libOffset, long cb, int dwLockType)
        {

        }

        public void Write(byte[] pv, int cb, IntPtr pcbWritten)
        {

        }
    }
}