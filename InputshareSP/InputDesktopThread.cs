using InputshareLib;
using InputshareLib.Displays;
using InputshareLib.Input;
using InputshareLibWindows.Output;
using InputshareLibWindows.Windows;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static InputshareLibWindows.Native.User32;

namespace InputshareSP
{

    /// <summary>
    /// Manages a thread that is always on the current input desktop
    /// </summary>
    internal sealed class InputDesktopThread
    {
        public event EventHandler DisplayConfigChanged;
        public event EventHandler<Edge> EdgeHit;
        public event EventHandler<bool> LeftMouseStateChanged;

        private Thread inputDeskThread;
        private BlockingCollection<Action> invokeQueue = new BlockingCollection<Action>();
        private CancellationTokenSource cancelToken;
        private DesktopMonitor deskMonitor;
        private WindowsOutputManager outMan = new WindowsOutputManager();

        //Callbacks are not always called from the same thread, so we need to invoke methods onto the dedicated thread to make sure
        //that they are executed on the correct desktop
        private Timer cursorMonitorTimer;
        private Timer displayUpdateTimer;

        public DisplayConfig CurrentDisplayConfig { get; private set; }
        private byte[] currentRawDisplayConfig = new byte[0];

        public bool Running { get; private set; }

        public InputDesktopThread()
        {
            CurrentDisplayConfig = GetDisplayConfig();
            currentRawDisplayConfig = CurrentDisplayConfig.ToBytes();
        }

        public void Start()
        {
            if (Running)
            {
                ISLogger.Write("Warning: Input desktop thread already running");
                return;
            }

            cancelToken = new CancellationTokenSource();
            
            //The desktop monitor thread cannot switch desktops, so we need to run it outside of our dedicated input desktop thread
            //and invoke the switchdesktop method onto the dedicated thread instead.
            deskMonitor = new DesktopMonitor();
            deskMonitor.DesktopSwitched += delegate (object sender, WindowsDesktop desktop) { invokeQueue.Add(() => { TrySwitchInputDesktop(); }); };
            cursorMonitorTimer = new Timer(delegate (object sync) { invokeQueue.Add(() => CheckCursorPosition()); }, null, 0, 50);
            displayUpdateTimer = new Timer(delegate (object sync) { invokeQueue.Add(() => CheckForDisplayUpdate()); }, null, 0, 1500);

            inputDeskThread = new Thread(InputDesktopLoop);
            inputDeskThread.Start();
        }

        public void InvokeAction(Action action)
        {
            invokeQueue.Add(action);
        }

        public void Stop()
        {
            if (Running)
            {
                ISLogger.Write("Warning: Input desktop thread not running");
                return;
            }

            cancelToken.Cancel();
            cursorMonitorTimer?.Dispose();
            displayUpdateTimer?.Dispose();
        }

        private void TrySwitchInputDesktop()
        {
            try
            {
                WindowsDesktop old = Desktop.CurrentDesktop;

                if (old == Desktop.InputDesktop)
                    return;

                Desktop.SwitchDesktop(Desktop.InputDesktop);
                ISLogger.Write("InputDesktopThread: {0} -> {1}", old, Desktop.CurrentDesktop);

            }
            catch (Exception ex)
            {
                ISLogger.Write("InputDesktopThread: Failed to switch to input desktop: " + ex.Message);
            }
        }

        private void InputDesktopLoop()
        {
            CurrentDisplayConfig = GetDisplayConfig();
            TrySwitchInputDesktop();

            Running = true;

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    invokeQueue.Take(cancelToken.Token).Invoke();
                }
                catch (OperationCanceledException) { }
                catch(Exception ex)
                {
                    ISLogger.Write("InputDesktopThread: unhandled exception on invoked method: " + ex.Message);
                    ISLogger.Write(ex.StackTrace);
                }
            }

            deskMonitor.Stop();
            ISLogger.Write("inputDesktopThread: Thread exited");
            Running = false;
        }

        public void SendInput(ISInputData data)
        {
            invokeQueue.Add(() => { outMan.Send(data); });
        }

        private bool lastSentMouseState = false;
        private void CheckCursorPosition()
        {
            if (!GetCursorPos(out POINT ptn))
            {
                ISLogger.Write("Failed to get cursor position! - " + new Win32Exception().Message);
                TrySwitchInputDesktop();
                return;
            }

            bool lState = (InputshareLibWindows.Native.User32.GetAsyncKeyState(System.Windows.Forms.Keys.LButton) & 0x8000) != 0;

            if(lState != lastSentMouseState)
            {
                LeftMouseStateChanged?.Invoke(this, lState);
                lastSentMouseState = lState;
            }

            if (ptn.X == CurrentDisplayConfig.VirtualBounds.Left)
                EdgeHit?.Invoke(this, Edge.Left);
            else if (ptn.X == CurrentDisplayConfig.VirtualBounds.Right - 1)
                EdgeHit?.Invoke(this, Edge.Right);
            else if (ptn.Y == CurrentDisplayConfig.VirtualBounds.Top)
                EdgeHit?.Invoke(this, Edge.Top);
            else if (ptn.Y == CurrentDisplayConfig.VirtualBounds.Bottom - 1)
                EdgeHit?.Invoke(this, Edge.Bottom);
        }

        private void CheckForDisplayUpdate()
        {
            DisplayConfig conf = GetDisplayConfig();

            if (!conf.ToBytes().SequenceEqual(currentRawDisplayConfig))
            {
                CurrentDisplayConfig = conf;
                currentRawDisplayConfig = CurrentDisplayConfig.ToBytes();
                DisplayConfigChanged?.Invoke(this, new EventArgs());
            }
        }

        public static DisplayConfig GetDisplayConfig()
        {
            int w = GetSystemMetrics(CX_VIRTUALSCREEN);
            int h = GetSystemMetrics(CY_VIRTUALSCREEN);
            Rectangle vBounds = new Rectangle(0, 0, 0, 0);

            List<Display> displays = new List<Display>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref W32Rect lprcMonitor, IntPtr dwData)
                {
                    MONITORINFOEX mi = new MONITORINFOEX();
                    mi.Size = Marshal.SizeOf(mi);
                    int displayIndex = 1;
                    if (GetMonitorInfo(hMonitor, ref mi))
                    {
                        Rectangle r = Rectangle.FromLTRB(mi.Monitor.left, mi.Monitor.top, mi.Monitor.right, mi.Monitor.bottom);
                        vBounds = Rectangle.Union(vBounds, r);
                        displays.Add(new Display(r, displayIndex, mi.DeviceName, (mi.Flags != 0)));
                        displayIndex++;
                    }
                    return true;
                }, IntPtr.Zero);

            return new DisplayConfig(vBounds, displays);
        }
    }
}
