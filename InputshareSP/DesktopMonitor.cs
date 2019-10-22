using InputshareLib;
using InputshareLibWindows;
using InputshareLibWindows.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static InputshareLibWindows.Windows.Desktop;

namespace InputshareSP
{
    public sealed class DesktopMonitor
    {
        public event EventHandler<WindowsDesktop> DesktopSwitched;
        public bool Monitoring { get; private set; }

        private HookWindow hookWnd;

        public DesktopMonitor()
        {
            hookWnd = new HookWindow("ISWndHook");
            hookWnd.HandleCreated += HookWnd_HandleCreated;
            Monitoring = true;
        }

        private void HookWnd_HandleCreated(object sender, EventArgs e)
        {
            hookWnd.InstallDesktopMonitor();
            hookWnd.DesktopSwitchEvent += HookWnd_DesktopSwitchEvent;
        }

        private void HookWnd_DesktopSwitchEvent(object sender, EventArgs e)
        {
            try
            {
                WindowsDesktop input = Desktop.InputDesktop;
                DesktopSwitched?.Invoke(this, input);
            }
            catch (Win32Exception ex)
            {
                ISLogger.Write("DesktopMonitor: Failed to open input desktop: " + ex.Message);
            }

        }

        public void Stop()
        {
            if (!hookWnd.Closed)
                hookWnd.CloseWindow();

            Monitoring = false;
        }
    }
}
