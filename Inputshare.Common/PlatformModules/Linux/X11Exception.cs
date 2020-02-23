using System;
using System.Collections.Generic;
using System.Text;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11;

namespace Inputshare.Common.PlatformModules.Linux
{
    internal class X11Exception : Exception
    {
        public X11Exception(string message) : base(message)
        {

        }

        internal static X11Exception Create(byte code, IntPtr xDisplay)
        {
            return new X11Exception(GetErrorString(xDisplay, code));
        }

        private static string GetErrorString(IntPtr xDisplay, byte code)
        {
            StringBuilder sb = new StringBuilder(256);
            XGetErrorText(xDisplay, code, sb, sb.Capacity);
            return sb.ToString();
        }
    }
}
