using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using static InputshareLibWindows.Native.AdvApi32;
using static InputshareLibWindows.Native.Wtsapi32;
using static InputshareLibWindows.Native.Kernel32;
using System.Diagnostics;
using System.Security.Principal;
using InputshareLibWindows.Native;

namespace InputshareLibWindows.Windows
{
    public static class Token
    {
        private static uint STANDARD_RIGHTS_READ = 0x00020000;
        private static uint TOKEN_QUERY = 0x0008;
        private static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

        /// <summary>
        /// Returns a token impersonating the logged in user
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        public static IntPtr GetUserToken()
        {
            IntPtr token = IntPtr.Zero;

            try
            {
                if (!Session.ConsoleSessionLoggedIn)
                    throw new NoUserSessionException();

                uint session = Session.ConsoleSessionId;
                if (!WTSQueryUserToken(session, out token))
                    throw new Win32Exception("WTSQueryUserTokenFailed: " + new Win32Exception().Message);

                return DuplicateToken(token);
            }
            finally
            {
                if(token != IntPtr.Zero)
                    CloseHandle(token);
            }
        }

        /// <summary>
        /// Returns an impersonation token for the LOCALSYSTEM user
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>"
        /// <exception cref="ProcessNotFoundException"></exception>"
        public static IntPtr GetSystemToken(uint sessionId)
        {
            IntPtr sysToken = IntPtr.Zero;
            IntPtr dupeToken = IntPtr.Zero;
            Process winLogon = null;

            try
            {
                Process[] procs = Process.GetProcessesByName("winlogon");

                

                foreach(var proc in procs)
                {
                    if (proc.SessionId == sessionId)
                        winLogon = proc;
                }

                if (winLogon == null)
                    throw new ProcessNotFoundException("Could not find winlogon in session " + sessionId);

                if (!OpenProcessToken(winLogon.Handle, (uint)TokenAccessLevels.Query | (uint)TokenAccessLevels.Impersonate | (uint)TokenAccessLevels.Duplicate, out sysToken))
                    throw new Win32Exception("OpenProcessToken failed - " + Marshal.GetLastWin32Error());

                dupeToken = DuplicateToken(sysToken);
                
                EnableSePrivilege(dupeToken);
                return dupeToken;
            }
            finally
            {
                if(sysToken != IntPtr.Zero)
                    CloseHandle(sysToken);

                winLogon?.Dispose();
            }
        }

        /// <summary>
        /// Closes a handle to a token
        /// </summary>
        /// <param name="token"></param>
        public static void CloseToken(IntPtr token)
        {
            Kernel32.CloseHandle(token);
        }

        /// <summary>
        /// Checks if the specified token is elevated
        /// </summary>
        /// <param name="token"></param>
        /// <exception cref="Win32Exception"></exception>"
        /// <returns></returns>
        public static TOKEN_ELEVATION_TYPE QueryElevation(IntPtr token)
        {
            uint pSize = 4;
            IntPtr buffer = Marshal.AllocHGlobal(4);

            if (!GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenElevationType, buffer, pSize, out uint retLen))
                throw new Win32Exception("GetTokenInformation failed: " + new Win32Exception().Message);

            return (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(buffer);
        }

        /// <summary>
        /// Returns a handle to the current process token
        /// </summary>
        /// <exception cref="Win32Exception"></exception>"
        /// <returns></returns>
        public static IntPtr GetCurrentProcessToken()
        {
            if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_READ, out IntPtr token))
                throw new Win32Exception("OpenProcessToken failed: " + new Win32Exception().Message);
            return token;
        }

        /// <summary>
        /// Returns an impersonation token for the specified token
        /// </summary>
        /// <param name="token"></param>
        /// <exception cref="Win32Exception"></exception>"
        /// <returns></returns>
        private static IntPtr DuplicateToken(IntPtr token)
        {
            SECURITY_ATTRIBUTES tokenAttribs = new SECURITY_ATTRIBUTES();
            tokenAttribs.nLength = Marshal.SizeOf(tokenAttribs);
            SECURITY_ATTRIBUTES threadAttribs = new SECURITY_ATTRIBUTES();
            threadAttribs.nLength = Marshal.SizeOf(threadAttribs);

            IntPtr dupeToken = IntPtr.Zero;

            if (!DuplicateTokenEx(token, 0x10000000, ref tokenAttribs, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenImpersonation, out dupeToken))
                throw new Win32Exception("DuplicateToken failed: " + new Win32Exception().Message);

            return dupeToken;
        }

        /// <summary>
        /// Enables the 'Act as part of the operating system' privilege on a token
        /// </summary>
        /// <param name="token"></param>
        /// <exception cref="Win32Exception"></exception>"
        private static void EnableSePrivilege(IntPtr token)
        {
            TOKEN_PRIVILEGES privs = new TOKEN_PRIVILEGES();
            privs.PrivilegeCount = 1;
            LUID seName = new LUID();

            if (!LookupPrivilegeValue(null, SE_DEBUG_NAME, ref seName))
                throw new Win32Exception("LookupPrivilegeValue failed: "+  new Win32Exception().Message);

            privs.Privileges = new LUID_AND_ATTRIBUTES[1];
            privs.Privileges[0].Luid = seName;
            privs.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

            if (!AdjustTokenPrivileges(token, false, ref privs, 0, IntPtr.Zero, IntPtr.Zero))
                throw new Win32Exception("AdjustTokenPrivileges failed: " + new Win32Exception().Message);
        }

        class NoUserSessionException : Exception
        {

        }

        class ProcessNotFoundException : Exception
        {
            public ProcessNotFoundException(string procName) : base("Process " + procName + " not found!")
            {
            }
        }
    }
}
