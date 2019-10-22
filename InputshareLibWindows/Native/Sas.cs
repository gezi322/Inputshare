using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace InputshareLibWindows.Native
{
    public static class Sas
    {
        [DllImport("sas.dll")]
        public static extern void SendSAS(bool asUser);
    }
}
