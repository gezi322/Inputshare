using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Inputshare.Tray
{
    /// <summary>
    /// Tray icon implementation for windows. not ideal as it requires a dedicated thread
    /// </summary>
    public class WinTrayIcon : ITrayIcon
    {
        public event EventHandler TrayIconClicked;
        public event EventHandler TrayIconDoubleClicked;

        private Thread _wndThread;
        private IntPtr _iconPtr;
        private IntPtr _windowHandle;
        private delegate IntPtr WndProcCallback(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);
        private WndProcCallback _wndProcDelegate;
        private NativeMethods.NOTIFYICONDATA _iconStruct;

        private const int WM_TRAYICONMESSAGE = 6666;
        private const int WM_DESTROYTRAYICON = 6667;
        private uint WM_TASKBARCREATED;

        /// <summary>
        /// Creates an instance of a tray icon for windows
        /// </summary>
        /// <param name="iconPath"></param>
        /// <returns></returns>
        public static WinTrayIcon Create(Bitmap icon)
        {
            WinTrayIcon tray = new WinTrayIcon();
            tray._iconPtr = icon.GetHicon();
            

            tray._wndThread = new Thread(() =>
            {
                tray._windowHandle = tray.CreateWindow();
                tray._iconStruct = tray.GetIconStruct();
                tray.AddTrayIcon();
                tray.MessageLoop();
            });

            tray._wndThread.Start();

            return tray;
        }

        private void MessageLoop()
        {
            int ret;
            NativeMethods.Win32Message msg;

            while ((ret = NativeMethods.GetMessage(out msg, _windowHandle, 0, 0)) > 0)
                WndProc(msg.hwnd, msg.message, msg.wParam, msg.lParam);
        }

        private IntPtr CreateWindow()
        {
            RegisterClass("InputshareTrayWnd");
            IntPtr hWnd = NativeMethods.CreateWindowEx(0, "InputshareTrayWnd", "", 0, 0, 0, 0, 0, IntPtr.Zero,
                IntPtr.Zero, Process.GetCurrentProcess().Handle, IntPtr.Zero);

            if (hWnd == default)
                throw new Win32Exception();

            return hWnd;
        }

        private void RegisterClass(string name)
        {
            _wndProcDelegate = new WndProcCallback(WndProc);

            NativeMethods.WNDCLASSEX cls = new NativeMethods.WNDCLASSEX
            {
                cbClsExtra = 0,
                cbSize = Marshal.SizeOf(typeof(NativeMethods.WNDCLASSEX)),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                lpszClassName = name,
                cbWndExtra = 0,
                hbrBackground = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hIcon = IntPtr.Zero,
                hIconSm = IntPtr.Zero,
                hInstance = Process.GetCurrentProcess().Handle,
                lpszMenuName = "",
                style = 0,
            };

            if (NativeMethods.RegisterClassEx(ref cls) == 0)
                throw new Win32Exception();
        }

        private NativeMethods.NOTIFYICONDATA GetIconStruct()
        {
            return new NativeMethods.NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NativeMethods.NOTIFYICONDATA)),
                hwnd = _windowHandle,
                uID = 100,
                uVersion = 4,
                uCallbackMessage = WM_TRAYICONMESSAGE,
                hIcon = _iconPtr,
                szTip = "",
                uFlags = 0x00000001 | 0x00000002 | 0x00000004
            };
        }

        private void AddTrayIcon()
        {
            if (!NativeMethods.Shell_NotifyIconW(0, ref _iconStruct))
                throw new Win32Exception();
        }

        private void RemoveTrayIcon()
        {
            NativeMethods.Shell_NotifyIconW(2, ref _iconStruct);
        }

        private IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
        {
            if(message == 1)
            {
                //Register the taskbarcreated message so that we can recreate the icon
                //if explorer crashes/restarts
                WM_TASKBARCREATED = NativeMethods.RegisterWindowMessageA("TaskbarCreated");
            }
            if(message == WM_TRAYICONMESSAGE)
            {
                if ((int)lParam == 0x0201)
                    TrayIconClicked?.Invoke(this, null);
                else if ((int)lParam == 0x0203)
                    TrayIconDoubleClicked?.Invoke(this, null);
            }else if(message == WM_DESTROYTRAYICON)
            {
                RemoveTrayIcon();
                NativeMethods.PostQuitMessage(0);
            }else if(message == WM_TASKBARCREATED)
            {
                AddTrayIcon();
            }

            return NativeMethods.DefWindowProc(hWnd, message, wParam, lParam);
        }

        

        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    NativeMethods.PostMessage(_windowHandle, WM_DESTROYTRAYICON, IntPtr.Zero, IntPtr.Zero);
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private class NativeMethods
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern void PostQuitMessage(int code);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            internal static extern IntPtr DefWindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern uint RegisterWindowMessageA(string message);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern UInt16 RegisterClassEx(ref WNDCLASSEX classEx);

            [DllImport("user32.dll")]
            internal static extern int GetMessage(out Win32Message message, IntPtr hwnd, uint min, uint max);

            [DllImport("user32.dll")]
            public static extern IntPtr LoadImageW(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoaD);

            [DllImport("user32.dll")]
            public static extern IntPtr GetActiveWindow();

            [DllImport("shell32.dll")]
            public static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATA data);
            public struct NOTIFYICONDATA
            {
                public int cbSize;
                public IntPtr hwnd;
                public int uID;
                public int uFlags;
                public int uCallbackMessage;
                public IntPtr hIcon;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string szTip;
                public int dwState;
                public int dwStateMask;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
                public string szInfo;
                public int uVersion;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
                public string szInfoTitle;
                public int dwInfoFlags;
            }


            internal struct Win32Message
            {
                public IntPtr hwnd;
                public uint message;
                public IntPtr wParam;
                public IntPtr lParam;
                public int time;
                public POINT pt;
            }

            internal struct POINT
            {
                internal POINT(int x, int y)
                {
                    X = x;
                    Y = y;
                }
                internal int X;
                internal int Y;
            }

            internal struct WNDCLASSEX
            {
                [MarshalAs(UnmanagedType.U4)]
                internal int cbSize;
                [MarshalAs(UnmanagedType.U4)]
                internal int style;
                internal IntPtr lpfnWndProc;
                internal int cbClsExtra;
                internal int cbWndExtra;
                internal IntPtr hInstance;
                internal IntPtr hIcon;
                internal IntPtr hCursor;
                internal IntPtr hbrBackground;
                [MarshalAs(UnmanagedType.LPStr)]
                internal string lpszMenuName;
                [MarshalAs(UnmanagedType.LPStr)]
                internal string lpszClassName;
                internal IntPtr hIconSm;
            }

            [DllImport("user32.dll", SetLastError = true, EntryPoint = "CreateWindowEx")]
            public static extern IntPtr CreateWindowEx(
           int dwExStyle,
           //UInt16 regResult,
           [MarshalAs(UnmanagedType.LPStr)]
           string lpClassName,
           string lpWindowName,
           UInt32 dwStyle,
           int x,
           int y,
           int nWidth,
           int nHeight,
           IntPtr hWndParent,
           IntPtr hMenu,
           IntPtr hInstance,
           IntPtr lpParam);
        }
    }
}
