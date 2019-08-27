﻿using InputshareLib;
using System;
using System.ComponentModel;
using System.Diagnostics;
using static InputshareLibWindows.Native.User32;

namespace InputshareLibWindows
{
    public class HookWindow : MessageWindow
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        public bool MouseHooked { get; private set; }
        public bool KeyboardHooked { get; private set; }
        public bool MonitoringClipboard { get; private set; }

        public delegate void ClipboardContentChangedDelegate(System.Windows.Forms.IDataObject data);
        
        private LowLevelHookCallback mouseCallback;
        private LowLevelHookCallback keyboardCallback;
        private ClipboardContentChangedDelegate clipboardChangeCallback;

        public IntPtr MouseHookProc { get; private set; }
        public IntPtr KeyboardHookProc { get; private set; }

        public HookWindow(string wndName) : base(wndName)
        {

        }

        public override void CloseWindow()
        {
            UninstallAllHooks();

            base.CloseWindow();
        }
        public void UninstallAllHooks()
        {
            if (MonitoringClipboard)
                UninstallClipboardMonitor();

            if (KeyboardHooked)
                UninstallKeyboardHook();

            if (MouseHooked)
                UninstallMouseHook();
        }

        #region keyboard hooks
        public void InstallKeyboardHook(LowLevelHookCallback callback)
        {
            if (KeyboardHooked)
                throw new InvalidOperationException("Keyboard hook already installed");

            InvokeAction(new Action(() => {
                HookKeyboard(callback);
            }));
        }

        private void UnhookKeyboard()
        {
            if (!KeyboardHooked)
                return;

            if (!UnhookWindowsHookEx(KeyboardHookProc))
                throw new Win32Exception();

            KeyboardHooked = false;
        }

        public void UninstallKeyboardHook()
        {
            if (!KeyboardHooked)
                throw new InvalidOperationException("Keyboard hook not installed");

            InvokeAction(new Action(() => { UnhookKeyboard(); }));
        }

        private void HookKeyboard(LowLevelHookCallback callback)
        {
            keyboardCallback = callback;

            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule curModule = curProcess.MainModule;
            KeyboardHookProc = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardCallback, GetModuleHandle(curModule.ModuleName), 0);

            if (KeyboardHookProc == IntPtr.Zero)
            {
                KeyboardHooked = false;
                throw new Win32Exception();
            }

            KeyboardHooked = true;
        }
        #endregion
        #region mouse hooks
        public void InstallMouseHook(LowLevelHookCallback callback)
        {
            if (MouseHooked)
                throw new InvalidOperationException("Mouse hook already installed");

            InvokeAction(new Action(() => {
                HookMouse(callback);
            }));
        }
        public void UninstallMouseHook()
        {
            if (!MouseHooked)
                throw new InvalidOperationException("Mouse hook not installed");

            InvokeAction(new Action(() => { UnhookMouse(); }));
        }
        private void HookMouse(LowLevelHookCallback callback)
        {
            mouseCallback = callback;

            using Process curProcess = Process.GetCurrentProcess();
                using ProcessModule curModule = curProcess.MainModule;
                    MouseHookProc = SetWindowsHookEx(WH_MOUSE_LL, mouseCallback, GetModuleHandle(curModule.ModuleName), 0);

            if (MouseHookProc == IntPtr.Zero)
            {
                MouseHooked = false;
                throw new Win32Exception();
            }

            MouseHooked = true;
        }
        private void UnhookMouse()
        {
            if (!MouseHooked)
                return;

            if (!UnhookWindowsHookEx(MouseHookProc))
                throw new Win32Exception();

            MouseHooked = false;
        }
        #endregion
        #region clipboard listener

        public void UninstallClipboardMonitor()
        {
            if (!MonitoringClipboard)
                throw new InvalidOperationException("Not monitoring clipboard");

            InvokeAction(() => { RemoveClipboardFormatListener(Handle); MonitoringClipboard = false; });
        }
        public void InstallClipboardMonitor(ClipboardContentChangedDelegate callback)
        {
            if (MonitoringClipboard)
                throw new InvalidOperationException("Already monitoring for clipboard changes");

            if (Closed)
                throw new InvalidOperationException("Window has been closed");
            clipboardChangeCallback = callback;
            InvokeAction(() => { AddClipboardMonitor(); });
        }
        private void AddClipboardMonitor()
        {
            if (!AddClipboardFormatListener(Handle))
                throw new Win32Exception();

            clipboardTimer.Start();
            MonitoringClipboard = true;
        }

        #endregion


        protected override IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_CLIPBOARDUPDATE && clipboardChangeCallback != null)
                ReadClipboardChange();
                

            return base.WndProc(hWnd, msg, wParam, lParam);
        }

        private readonly Stopwatch clipboardTimer = new Stopwatch();
        private void ReadClipboardChange()
        {
            if (ignoreCbChange)
            {
                clipboardTimer.Restart();
                ignoreCbChange = false;
                return;
            }

            if(clipboardTimer.ElapsedMilliseconds < 500)
            {
                return;
            }
            try
            {
                var obj = System.Windows.Forms.Clipboard.GetDataObject();
                if(obj == null)
                {
                    ISLogger.Write("Failed to read clipboard data.");
                    return;
                }

                clipboardChangeCallback(obj);

                clipboardTimer.Restart();
            }catch(Exception ex)
            {
                ISLogger.Write("Error reading clipboard data: {0}", ex.Message);
            }
            
        }
    }
}
