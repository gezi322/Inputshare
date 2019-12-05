using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.Windows;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static InputshareLibWindows.Native.AdvApi32;
namespace InputshareService
{
    public static class ProcessLauncher
    {
        
        /// <summary>
        /// Launches inputshareSP
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="desktop"></param>
        /// <param name="createConsole"></param>
        /// <param name="ipcHost"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="CreateProcessException"></exception>"
        public static Process LaunchSP(SPMode mode, WindowsDesktop desktop, bool createConsole, AnonIpcHost ipcHost, IntPtr token)
        {
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = desktop.ToString();
            CreateBlankAttribs(out SECURITY_ATTRIBUTES tokenAttribs, out SECURITY_ATTRIBUTES threadAttribs);

            CreateProcessFlags flags = CreateProcessFlags.REALTIME_PRIORITY_CLASS;

            if (!createConsole)
                flags |= CreateProcessFlags.CREATE_NO_WINDOW;

            string cmd = " " + mode.ToString() + " " + ipcHost.WriteStringHandle + " " + ipcHost.ReadStringHandle;

            if (!CreateProcessAsUser(token, AppDomain.CurrentDomain.BaseDirectory + "\\inputsharesp.exe",
                cmd,
                ref tokenAttribs,
                ref threadAttribs,
                true,
                (uint)flags,
                IntPtr.Zero,
                AppDomain.CurrentDomain.BaseDirectory,
                ref si,
                out pi))
            {
                throw new CreateProcessException(new Win32Exception().Message);
            }

            return Process.GetProcessById(pi.dwProcessId);
        }
        private static void CreateBlankAttribs(out SECURITY_ATTRIBUTES tokenAttribs, out SECURITY_ATTRIBUTES threadAttribs)
        {
            tokenAttribs = new SECURITY_ATTRIBUTES();
            tokenAttribs.nLength = Marshal.SizeOf(tokenAttribs);
            threadAttribs = new SECURITY_ATTRIBUTES();
            threadAttribs.nLength = Marshal.SizeOf(threadAttribs);
        }


        public enum SPMode
        {
            Default,
            Clipboard
        }

        public class CreateProcessException : Exception
        {
            public CreateProcessException(string message) : base(message)
            {

            }
        }
    }
}
