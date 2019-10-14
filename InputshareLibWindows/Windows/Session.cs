using InputshareLib;
using InputshareLibWindows.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using static InputshareLibWindows.Native.Wtsapi32;

namespace InputshareLibWindows.Windows
{
    public static class Session
    {
        /// <summary>
        /// The ID of the current console session
        /// </summary>
        public static uint ConsoleSessionId { get => Kernel32.WTSGetActiveConsoleSessionId(); }

        /// <summary>
        /// True if a user is logged into a console session
        /// </summary>
        public static bool ConsoleSessionLoggedIn { get => GetSessionState(ConsoleSessionId) == WTS_CONNECTSTATE_CLASS.WTSActive; }

        /// <summary>
        /// Gets the state of the console session
        /// </summary>
        public static WTS_CONNECTSTATE_CLASS ConsoleSessionState { get => GetSessionState(ConsoleSessionId); }


        private static WTS_CONNECTSTATE_CLASS GetSessionState(uint sessionId)
        {
            IntPtr buffer = IntPtr.Zero;

            try
            {
                if (!WTSQuerySessionInformation(Wtsapi32.WTS_CURRENT_SERVER_HANDLE, (int)sessionId, WTS_INFO_CLASS.WTSConnectState, out buffer, out int bytesReturned))
                    throw new Win32Exception("WTSQuerySessionInformation failed");

                return (WTS_CONNECTSTATE_CLASS)Marshal.ReadInt32(buffer);
            }
            finally
            {
                if(buffer != IntPtr.Zero)
                    WTSFreeMemory(buffer);
            }
        }
    }
}
