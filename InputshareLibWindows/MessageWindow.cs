using InputshareLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using static InputshareLibWindows.Native.User32;
using static InputshareLibWindows.Native.WindowMessages;

namespace InputshareLibWindows
{

    /// <summary>
    /// Represents a message only window. Used for keyboard, mouse and clipboard event hooks.
    /// Hooks should only be installed after the HandleCreated event has been fired
    /// </summary>
    public class MessageWindow
    {
        public event EventHandler HandleCreated;
        public event EventHandler WindowDestroyed;

        public bool Closed { get; protected set; } = false;

        private const int INPUTSHARE_EXECMETHOD = 1333;

        private WNDCLASSEX wndClass;

        private delegate IntPtr Procdel(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private Procdel WndProcDelegate;

        private Thread wndThread;
        private Queue<Action> invokeQueue;
        private CancellationTokenSource cancelToken;

        protected bool ignoreCbChange = false;
        protected IntPtr Handle { get; private set; }

        protected string WindowName { get; }

        public MessageWindow(string wndName)
        {
            WindowName = wndName;
            cancelToken = new CancellationTokenSource();
            wndThread = new Thread(() =>
            {
                CreateWindow();
            });
            wndThread.SetApartmentState(ApartmentState.STA);
            wndThread.Start();
        }

        public virtual void CloseWindow()
        {
            if (Handle == IntPtr.Zero)
                throw new Exception("Window handle does not exist");

            PostMessage(new HandleRef(this, Handle), WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            cancelToken.Cancel();
        }
       
        public virtual void SetClipboardData(System.Windows.Forms.DataObject data)
        {
            if (Closed)
                throw new InvalidOperationException("Window has been closed");

            InvokeAction(new Action(() => {

                for(int i = 0; i < 10; i++)
                {
                    try
                    {
                        ignoreCbChange = true;
                        System.Windows.Forms.Clipboard.SetDataObject(data, false);
                        GC.Collect();
                        return;
                    }catch
                    {
                        Thread.Sleep(25);
                    }
                }

                ISLogger.Write("Failed to set clipboard data!");
            }));
        }

        private void CreateWindow()
        {
            invokeQueue = new Queue<Action>();

            WndProcDelegate = WndProc;
            CreateClass();

            Handle = CreateWindowEx(0,
                wndClass.lpszClassName,
                "ismsg",
                0,
                0,
                0,
                0,
                0,
                new IntPtr(-3),
                IntPtr.Zero,
                Process.GetCurrentProcess().Handle,
                IntPtr.Zero);

            if (Handle == IntPtr.Zero)
                throw new Win32Exception();

            OnHandleCreated();
            WndThreadLoop();
        }

        private void WndThreadLoop()
        {
            MSG msg = new MSG();
            int ret;
            while ((ret = GetMessage(out msg, Handle, 0, 0)) != 0)
            {
                if (ret == -1)
                {
                    break;
                }
                else
                {
                    DispatchMessage(ref msg);
                }
            }

            ISLogger.Write("Destroyed window {0}", WindowName);

            Closed = true;
            DestroyWindow(Handle);
            OnWindowDestroyed();
            UnregisterClassA(wndClass.lpszClassName, Process.GetCurrentProcess().Handle);
        }

        protected void InvokeAction(Action invoke)
        {
            if (Closed)
                throw new InvalidOperationException("Window has been closed");

            if (Handle == IntPtr.Zero)
                throw new InvalidOperationException("Window handle does not exist");

            invokeQueue.Enqueue(invoke);
            PostMessage(new HandleRef(this, Handle), INPUTSHARE_EXECMETHOD, IntPtr.Zero, IntPtr.Zero);
        }

        private void CreateClass()
        {
            wndClass = new WNDCLASSEX
            {
                cbClsExtra = 0,
                cbSize = Marshal.SizeOf(typeof(WNDCLASSEX)),
                cbWndExtra = 0,
                hbrBackground = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hIcon = IntPtr.Zero,
                hIconSm = IntPtr.Zero,
                hInstance = Process.GetCurrentProcess().Handle,
                lpszClassName = GenerateClassName(),
                lpszMenuName = null,
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProcDelegate)
            };
            
            if(RegisterClassEx(ref wndClass) == 0)
            {
                throw new Win32Exception();
            }
        }

        private string GenerateClassName()
        {
            Random r = new Random();
            return "isclass_" + r.Next(0, int.MaxValue) + "_" + r.Next(0, int.MaxValue);
        }

        private void OnWindowDestroyed()
        {
            WindowDestroyed?.Invoke(this, null);
        }

        protected virtual void OnHandleCreated()
        {
            HandleCreated?.Invoke(this, null);
        }

        protected virtual IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {

            if (msg == INPUTSHARE_EXECMETHOD)
            {
                if(invokeQueue != null && invokeQueue.Count > 0)
                {
                    invokeQueue.TryDequeue(out Action invoke);

                    invoke?.Invoke();
                }

                return IntPtr.Zero;
            }else if(msg == WM_CLOSE)
            {
                PostQuitMessage(0);
            }

            return DefWindowProcA(hWnd, msg, wParam, lParam);
        }
    }
}
