using InputshareLib;
using InputshareLib.Displays;
using InputshareLib.PlatformModules.Displays;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static InputshareLibWindows.Native.User32;

namespace InputshareLibWindows.PlatformModules.Displays
{
    public class WindowsDisplayManager : DisplayManagerBase
    {
        private System.Threading.Timer displayUpdateTimer;
        private System.Threading.Timer cursorUpdateTimer;

        public WindowsDisplayManager()
        {
            UpdateConfigManual();
        }

        protected override void OnStart()
        {
            displayUpdateTimer = new System.Threading.Timer(UpdateTimerCallback, null, 0, 1500);
            cursorUpdateTimer = new System.Threading.Timer(CursorTimerCallback, null, 0, 50);
            UpdateConfigManual();
        }

        protected override void OnStop()
        {
            cursorUpdateTimer?.Dispose();
            displayUpdateTimer.Dispose();
        }

        private void UpdateTimerCallback(object sync)
        {
            CheckForUpdate();
        }

        private void CursorTimerCallback(object state)
        {
            GetCursorPos(out POINT ptn);

            if (ptn.X == CurrentConfig.VirtualBounds.Left)
                OnEdgeHit(Edge.Left);
            else if (ptn.X == CurrentConfig.VirtualBounds.Right - 1)
                OnEdgeHit(Edge.Right);
            else if (ptn.Y == CurrentConfig.VirtualBounds.Top)
                OnEdgeHit(Edge.Top);
            else if (ptn.Y == CurrentConfig.VirtualBounds.Bottom - 1)
                OnEdgeHit(Edge.Bottom);
        }

        public void CheckForUpdate()
        {
            DisplayConfig conf = GetDisplayConfig();

            if (!conf.Equals(CurrentConfig))
                OnConfigUpdated(conf);
        }

        private Display GetDisplay(int index)
        {
            if (Screen.AllScreens[index] == null)
                throw new ArgumentException("Display does not exist");

            Display indexDisplay = null;

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref W32Rect lprcMonitor, IntPtr dwData)
                {
                    MONITORINFOEX mi = new MONITORINFOEX();
                    mi.Size = Marshal.SizeOf(mi);
                    int displayIndex = 1;
                    if (GetMonitorInfo(hMonitor, ref mi))
                    {
                        if (displayIndex == index)
                        {
                            Rectangle r = new Rectangle(mi.Monitor.left, mi.Monitor.bottom,
                         Math.Abs(mi.Monitor.right - mi.Monitor.left), Math.Abs(mi.Monitor.top - mi.Monitor.bottom));
                            indexDisplay = new Display(r, index, mi.DeviceName, mi.Flags > 0);
                            return false;
                        }

                        displayIndex++;
                    }
                    return true;
                }, IntPtr.Zero);

            if (indexDisplay == null)
                throw new Exception("Could not find monitor at index " + index);

            return indexDisplay;
        }

        private DisplayConfig GetDisplayConfig()
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

        public override void UpdateConfigManual()
        {
            OnConfigUpdated(CurrentConfig);
        }


    }
}
