using InputshareLib.PlatformModules.Windows.Native;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static InputshareLib.PlatformModules.Windows.Native.User32;

namespace InputshareLib.PlatformModules.Windows
{
    /// <summary>
    /// A win32 message only window
    /// </summary>
    internal class WinMessageWindow : IDisposable
    {
        internal event EventHandler<Win32Message> MessageRecevied;

        internal readonly string WindowName;
        internal IntPtr Handle { get; private set; }

        private delegate IntPtr wndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private SemaphoreSlim _creationWaitHandle;
        private Thread _wndThread;
        private wndProcDelegate _wndProc;
        private BlockingCollection<Action> _invokeQueue;

        private WinMessageWindow(string windowName)
        {
            WindowName = windowName;
            _invokeQueue = new BlockingCollection<Action>();
            _wndProc = new wndProcDelegate(InternalWndProc);
        }

        /// <summary>
        /// Creates a message window for receiving win32 messages
        /// </summary>
        /// <returns></returns>
        internal static async Task<WinMessageWindow> CreateWindowAsync(string windowName, int timeout = 5000)
        {
            WinMessageWindow wnd = new WinMessageWindow(windowName);
            SemaphoreSlim waitHandle = new SemaphoreSlim(0, 1);

            //Create the window thread
            wnd._creationWaitHandle = waitHandle;
            wnd._wndThread = new Thread(() => wnd.InitWindow());
            wnd._wndThread.SetApartmentState(ApartmentState.STA);
            wnd._wndThread.Start();

            if (!await waitHandle.WaitAsync(timeout))
                throw new Exception("Timed out waiting for window creation event"); //TODO

            return wnd;
        }

        /// <summary>
        /// Creates the window and message loop
        /// </summary>
        private void InitWindow()
        {
            Ole32.OleInitialize(IntPtr.Zero);
            Handle = CreateWindow();

            _creationWaitHandle.Release();
            Logger.Write($"Window {WindowName} created");

            Win32Message msg;
            int ret;
            while ((ret = GetMessage(out msg, Handle, 0, 0)) != 0)
            {
                if (ret == -1)
                    break;

                

                DispatchMessage(ref msg);
            }
        }

        private IntPtr CreateWindow()
        {
            WNDCLASSEX cls = RegisterWindowClass();
            IntPtr wnd = CreateWindowEx(0x00000020 | 0x00000080, cls.lpszClassName, WindowName, 0, 0, 0, 100, 100, IntPtr.Zero, IntPtr.Zero,
                Process.GetCurrentProcess().Handle, IntPtr.Zero);

            if (wnd == IntPtr.Zero)
                throw new Win32Exception();

            return wnd;
        }

        private WNDCLASSEX RegisterWindowClass()
        {
            WNDCLASSEX cls = new WNDCLASSEX
            {
                cbClsExtra = 0,
                cbSize = Marshal.SizeOf(typeof(WNDCLASSEX)),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                lpszClassName = WindowName + "_cls",
                cbWndExtra = 0,
                hbrBackground = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hIcon = IntPtr.Zero,
                hIconSm = IntPtr.Zero,
                hInstance = Process.GetCurrentProcess().Handle,
                lpszMenuName = "",
                style = 0,
            };

           ushort ret = RegisterClassEx(ref cls);
           if (ret == 0)
              throw new Win32Exception();

            return cls;
        }

        protected IntPtr InternalWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if ((Win32MessageCode)msg == Win32MessageCode.ISMSG_EXECACTION)
            {
                if (_invokeQueue.TryTake(out var invoke, 1000))
                    invoke();
            }
                    


            MessageRecevied?.Invoke(this, new Win32Message { hwnd = hWnd, message = msg, wParam = wParam, lParam = lParam });
            return WndProc(hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// Window message callback
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        protected virtual IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return DefWindowProcA(hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// Runs an action on the window thread
        /// </summary>
        /// <param name="invoke"></param>
        internal void InvokeAction(Action invoke)
        {
            _invokeQueue.Add(invoke);
            PostMessage(Handle, Win32MessageCode.ISMSG_EXECACTION, IntPtr.Zero, IntPtr.Zero);
        }


        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    PostMessage(Handle, Win32MessageCode.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
                disposedValue = true;
            }
        }

        /// <summary>
        /// Closes the window and cleans up
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
