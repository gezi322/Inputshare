using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using InputshareLib;
using InputshareLib.Displays;
using InputshareLibWindows;
using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.Output;
using InputshareLibWindows.Windows;
using InputshareSP.DefaultHost;
using static InputshareLibWindows.Native.User32;

namespace InputshareSP.DefaultHost
{
    internal static class SPDefaultHost
    {
        private static ActiveDesktopThread deskThread;
        private static ActiveDesktopThread outputDeskThread;
        private static HookWindow hWindow;
        private static AnonIpcClient iClient;
        private static WindowsOutputManager outMan = new WindowsOutputManager();

        private static Timer cursorPosUpdateTimer;
        private static Timer displayCheckTimer;
        private static DisplayConfig currentDisplayConfig = new DisplayConfig(new System.Drawing.Rectangle(0, 0, 0, 0), new List<Display>());

        internal static void Init(AnonIpcClient client)
        {
            ISLogger.SetLogFileName("InputshareSP_DefaultHost.log");
            ISLogger.Write("Starting SP default host");
            iClient = client;

            outMan = new WindowsOutputManager();
            outMan.Start();

            deskThread = new ActiveDesktopThread();
            deskThread.Invoke(() => { Desktop.SwitchDesktop(Desktop.InputDesktop); });

            outputDeskThread= new ActiveDesktopThread();
            outputDeskThread.Invoke(() => { Desktop.SwitchDesktop(Desktop.InputDesktop); });

            hWindow = new HookWindow("SPDesktopWatcher");
            hWindow.InitWindow();

            hWindow.InstallDesktopMonitor();
            hWindow.DesktopSwitchEvent += HWindow_DesktopSwitchEvent;

            iClient.Disconnected += IClient_Disconnected;
            iClient.InputReceived += IClient_InputReceived;

            deskThread.Invoke(() => { GetDisplayConfig(); });
            cursorPosUpdateTimer = new Timer((object o) => { CheckCursorPosition(); }, 0, 0, 50);
            displayCheckTimer = new Timer((object o) => { CheckForDisplayConfigChange(); }, 0, 0, 1500);
            while (true)
                Thread.Sleep(5000);
        }

        private static void IClient_InputReceived(object sender, InputshareLib.Input.ISInputData input)
        {
            outputDeskThread.Invoke(() => { outMan.Send(input); });
        }

        private static void IClient_Disconnected(object sender, string e)
        {
            ISLogger.Write("Lost connection to the Inputshare service... exiting");
            Exit();
        }

        private static void HWindow_DesktopSwitchEvent(object sender, EventArgs e)
        {
            deskThread.InvokeSwitchDesktop();
            outputDeskThread.InvokeSwitchDesktop();
        }

        private static void CheckCursorPosition()
        {
            deskThread.Invoke(() => {
                GetCursorPos(out POINT ptn);

                if (ptn.X == currentDisplayConfig.VirtualBounds.Left)
                    OnEdgeHit(Edge.Left);
                else if (ptn.X == currentDisplayConfig.VirtualBounds.Right - 1)
                    OnEdgeHit(Edge.Right);
                else if (ptn.Y == currentDisplayConfig.VirtualBounds.Top)
                    OnEdgeHit(Edge.Top);
                else if (ptn.Y == currentDisplayConfig.VirtualBounds.Bottom - 1)
                    OnEdgeHit(Edge.Bottom);
            });
        }

        private static void OnEdgeHit(Edge edge)
        {
            iClient.SendEdgeHit(edge);
        }

        private static DisplayConfig GetDisplayConfig()
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

        private static void CheckForDisplayConfigChange()
        {
            DisplayConfig conf = GetDisplayConfig();

            if (!conf.Equals(currentDisplayConfig))
            {
                currentDisplayConfig = conf;
                iClient.SendDisplayConfig(conf);
            }
                
        }


        private static void Exit()
        {
            deskThread?.Dispose();
            iClient?.Dispose();
            hWindow?.CloseWindow();
        }
    }
}
