using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using static InputshareLibWindows.Native.User32;
using static InputshareLibWindows.Windows.Desktop;
using static InputshareLibWindows.Native.Kernel32;
using InputshareLibWindows.Native;

namespace InputshareLibWindows.Windows
{
    public static class Desktop
    {
        /// <summary>
        /// Gets the desktop assigned to the current thread
        /// </summary>
        /// /// <exception cref="Win32Exception"></exception>"
        public static WindowsDesktop CurrentDesktop { get => GetThreadDesktop(); }

        /// <summary>
        /// Gets the current input desktop
        /// </summary>
        /// <exception cref="Win32Exception"></exception>"
        public static WindowsDesktop InputDesktop { get => GetInputDesktop(); }

        /// <summary>
        /// Switches the current thread to the specified desktop
        /// </summary>
        /// <exception cref="Win32Exception"></exception>
        /// <param name="desktop"></param>
        public static void SwitchDesktop(WindowsDesktop desktop)
        {
            IntPtr wl = OpenDesktop(desktop.ToString(), 0, false, ACCESS_MASK.MAXIMUM_ALLOWED);
            try
            {
                if (wl == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (!SetThreadDesktop(wl))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (wl != IntPtr.Zero)
                    CloseDesktop(wl);
            }
        }


        /// <summary>
        /// Gets the current input desktop
        /// </summary>
        /// <exception cref="Win32Exception"></exception>
        /// <returns></returns>
        private static WindowsDesktop GetInputDesktop()
        {
            IntPtr hDesk = OpenInputDesktop(0, false, ACCESS_MASK.MAXIMUM_ALLOWED);

            try
            {
                if (hDesk == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                Enum.TryParse(typeof(WindowsDesktop), GetDesktopName(hDesk), out object desk);
                return (WindowsDesktop)desk;
            }
            finally
            {
                if (hDesk != IntPtr.Zero)
                    CloseDesktop(hDesk);
            }
        }

        /// <summary>
        /// Gets the current thread desktop
        /// </summary>
        /// <exception cref="Win32Exception"></exception>
        /// <returns></returns>
        private static WindowsDesktop GetThreadDesktop()
        {
            IntPtr hDesk = User32.GetThreadDesktop(GetCurrentThreadId());

            try
            {
                if (hDesk == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                string name = GetDesktopName(hDesk);
                Enum.TryParse<WindowsDesktop>(name, true, out WindowsDesktop desk);
                return desk;
            }
            finally
            {
                if (hDesk != IntPtr.Zero)
                    CloseDesktop(hDesk);
            }
        }

        /// <summary>
        /// Gets the name of a desktop object from a pointer
        /// </summary>
        /// <param name="desktop"></param>
        /// <returns>Desktop name</returns>
        /// <exception cref="Win32Exception"></exception>
        private static string GetDesktopName(IntPtr desktop)
        {
            GetUserObjectInformation(desktop, UOI_NAME, null, 0, out uint len);
            byte[] buff = new byte[len];
            if (!GetUserObjectInformation(desktop, UOI_NAME, buff, len, out len))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return Encoding.UTF8.GetString(buff, 0, (int)len - 1);
        }

        /// <summary>
        /// Standard windows desktop objects
        /// </summary>
    }


    public enum WindowsDesktop
    {
        Other = 0,
        Winlogon = 1,
        Default = 2,
        Screensaver = 3,
    }
}
