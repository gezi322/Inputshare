using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Inputshare.Common.PlatformModules.Linux.Native
{
    public static class Libc
    {
        public struct timeval
        {
            public long tv_sec;
            public long tv_usec;
        }

        public struct fd_set
        {
            public int fd_count;
            public int[] array;
        }

        [DllImport("libc.so.6")]
        public static extern int select(int __nfds, IntPtr read, IntPtr write, IntPtr except, ref timeval timeout);
    }
}
