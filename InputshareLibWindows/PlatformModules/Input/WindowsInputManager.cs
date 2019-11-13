using InputshareLib;
using InputshareLib.Input;
using InputshareLib.Input.Hotkeys;
using InputshareLib.Input.Keys;
using InputshareLib.PlatformModules.Input;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static InputshareLibWindows.Native.User32;
using static InputshareLibWindows.Native.WindowMessages;

namespace InputshareLibWindows.PlatformModules.Input
{
    public class WindowsInputManager : InputManagerBase
    {

        public override bool InputBlocked { get; protected set; }
        public override MouseInputMode MouseRecordMode { get; protected set; }

        public override bool LeftMouseDown { get => (GetAsyncKeyState(System.Windows.Forms.Keys.LButton) & 0x8000) != 0; }

        public override event EventHandler<ISInputData> InputReceived;

        private HookWindow hookWnd;
        private readonly IntPtr hookBlockValue = new IntPtr(-1);
        private Task inputTranslateTask;
        private BlockingCollection<NativeInputData> inputTranslateQueue;
        private CancellationTokenSource cancelSource;

        private Task mouseRecordBufferTask;
        private CancellationTokenSource mouseBufferTaskCancelSource;

        private short cursorBufferX = 0;
        private short cursorBufferY = 0;
        private int bufferRecordInterval = 16;
        private Timer mPosUpdateTimer;
        private struct NativeInputData
        {
            public int code;
            public MSLLHOOKSTRUCT mStruct;
            public KBDLLHOOKSTRUCT kStruct;
        }

        protected override void OnStart()
        {
            SetProcessDPIAware();
            hookWnd = new HookWindow("Inputmanager window");
            hookWnd.HandleCreated += HookWnd_HandleCreated;
            hookWnd.WindowDestroyed += HookWnd_WindowDestroyed;
            hookWnd.InitWindow();
            inputTranslateQueue = new BlockingCollection<NativeInputData>();
            cancelSource = new CancellationTokenSource();
            inputTranslateTask = new Task(InputTranslateLoop, cancelSource.Token);
            inputTranslateTask.Start();
            mPosUpdateTimer = new Timer(MousePosUpdateCallback, 0, 250, 250);

            SetMouseInputMode(MouseInputMode.Realtime);
        }

        protected override void OnStop()
        {
            if (!hookWnd.Closed)
            {
                hookWnd.CloseWindow();
            }

            mouseBufferTaskCancelSource?.Cancel();
            cancelSource?.Cancel();
            SetInputBlocked(false);
            hookWnd = null;
        }


        public override void SetMouseInputMode(MouseInputMode mode, int interval = 0)
        {
            

            if (mode == MouseRecordMode)
            {
                if (interval >= 1 && interval <= 1000 && interval != bufferRecordInterval)
                {
                    bufferRecordInterval = 1000 / interval;
                    ISLogger.Write("Cursor record mode set to buffered at {0}/S", 1000/bufferRecordInterval);
                }

                return;
            }
                

            if(mode == MouseInputMode.Buffered)
            {
                mouseBufferTaskCancelSource = new CancellationTokenSource();
                mouseRecordBufferTask = new Task(CursorBufferLoop, mouseBufferTaskCancelSource.Token);
                mouseRecordBufferTask.Start();
                ISLogger.Write("Cursor record mode set to buffered at {0}/S", 1000/bufferRecordInterval);
            }else if(mode == MouseInputMode.Realtime)
            {
                mouseBufferTaskCancelSource?.Cancel();
                ISLogger.Write("Cursor record mode set to realtime");
            }

            MouseRecordMode = mode;
        }

        private void CursorBufferLoop()
        {
            while (!mouseBufferTaskCancelSource.IsCancellationRequested)
            {
                InputReceived?.Invoke(this, new ISInputData(ISInputCode.IS_MOUSEMOVERELATIVE, cursorBufferX, cursorBufferY));
                cursorBufferX = 0;
                cursorBufferY = 0;
                Thread.Sleep(bufferRecordInterval);
            }
        }

        public override void SetInputBlocked(bool block)
        {
            if (block)
                BlockInput();
            else
                UnblockInput();
        }

        private void BlockInput()
        {
            InputBlocked = true;
            GetCursorPos(out lastMousePos);
        }

        private void MousePosUpdateCallback(object sync)
        {
            if(InputBlocked)
                GetCursorPos(out lastMousePos);
        }

        private void UnblockInput()
        {
            InputBlocked = false;
        }

        private void HookWnd_WindowDestroyed(object sender, EventArgs e)
        {
            ISLogger.Write("window destroyed");
        }

        private void HookWnd_HandleCreated(object sender, EventArgs e)
        {
            if (!Settings.DEBUG_DISABLEHOOKS)
            {
                hookWnd.InstallKeyboardHook(KeyboardCallback);
                hookWnd.InstallMouseHook(MouseCallback);
            }
        }

        #region callbacks

        private readonly MSLLHOOKSTRUCT mouseData = new MSLLHOOKSTRUCT();
        private POINT lastMousePos = new POINT();
        private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Marshal.PtrToStructure(lParam, mouseData);

            if (InputBlocked && ((mouseData.flags & 1) != 1))
            {
                inputTranslateQueue.Add(new NativeInputData { code = (int)wParam, mStruct = mouseData });
                return hookBlockValue;
            }
            else
            {
                lastMousePos = mouseData.pt;
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }
        }

        private readonly KBDLLHOOKSTRUCT keyboardData = new KBDLLHOOKSTRUCT();
        private IntPtr KeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Marshal.PtrToStructure(lParam, keyboardData);
            CheckForHotkey((int)wParam);

            if (InputBlocked)
            {
                inputTranslateQueue.Add(new NativeInputData { code = (int)wParam, kStruct = keyboardData });
                return hookBlockValue;
            }
            else
            {
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }
        }

        #endregion

        #region input translation thread
        private void InputTranslateLoop()
        {
            try
            {
                while (!cancelSource.IsCancellationRequested)
                {
                    NativeInputData data = inputTranslateQueue.Take(cancelSource.Token);
                    if (data.code >= 512 && data.code <= 524)
                    {
                        ConvertMouseInputData(data.code, ref data.mStruct); ;
                    }
                    else if (data.code >= 0x0100 && data.code <= 0x0109)
                    {
                        ConvertKeyboardInputData(data.code, ref data.kStruct); ;
                    }
                }
            }
            catch (OperationCanceledException) { }
            
        }

        private void ConvertMouseInputData(int code, ref MSLLHOOKSTRUCT mouseStruct)
        {
            ISInputData translatedData = null;
            switch (code)
            {
                case WM_MOUSEMOVE:
                    {
                        short xRel = (short)(mouseData.pt.X - lastMousePos.X);
                        short yRel = (short)(mouseData.pt.Y - lastMousePos.Y);
                        translatedData = new ISInputData(ISInputCode.IS_MOUSEMOVERELATIVE, xRel, yRel);
                        break;
                    }
                case WM_LMOUSEDOWN:
                    {
                        translatedData = new ISInputData(ISInputCode.IS_MOUSELDOWN, 0, 0);
                        break;
                    }
                case WM_LMOUSEUP:
                    {
                        translatedData = new ISInputData(ISInputCode.IS_MOUSELUP, 0, 0);
                        break;
                    }
                case WM_RMOUSEDOWN:
                    {
                        translatedData = new ISInputData(ISInputCode.IS_MOUSERDOWN, 0, 0);
                        break;
                    }
                case WM_RMOUSEUP:
                    {
                        translatedData = new ISInputData(ISInputCode.IS_MOUSERUP, 0, 0);
                        break;
                    }
                case WM_MBUTTONDOWN:
                    {
                        translatedData = new ISInputData(ISInputCode.IS_MOUSEMDOWN, 0, 0);
                        break;
                    }
                case WM_MBUTTONUP:
                    {
                        translatedData = new ISInputData(ISInputCode.IS_MOUSEMUP, 0, 0);
                        break;
                    }
                case WM_MOUSEWHEEL:
                    {
                        //TODO - implement X axis scrolling
                        translatedData = new ISInputData(ISInputCode.IS_MOUSEYSCROLL, unchecked((short)((long)mouseStruct.mouseData >> 16)), 0);
                        break;
                    }
                case WM_XBUTTONDOWN:
                    {
                        translatedData = new ISInputData(ISInputCode.IS_MOUSEXDOWN, unchecked((short)((long)mouseStruct.mouseData >> 16)), 0);
                        break;
                    }
                case WM_XBUTTONUP:
                    {
                        translatedData = new ISInputData(ISInputCode.IS_MOUSEXUP, unchecked((short)((long)mouseStruct.mouseData >> 16)), 0);
                        break;
                    }
            }

            if(translatedData.Code == ISInputCode.IS_MOUSEMOVERELATIVE && MouseRecordMode != MouseInputMode.Realtime)
            {
                cursorBufferX += translatedData.Param1;
                cursorBufferY += translatedData.Param2;
                return;
            }

            if (translatedData != null)
                InputReceived?.Invoke(this, translatedData);
        }

        private void ConvertKeyboardInputData(int code, ref KBDLLHOOKSTRUCT keyboardStruct)
        {
            WindowsVirtualKey key = (WindowsVirtualKey)keyboardStruct.vkCode;

            if (Settings.DEBUG_PRINTINPUTKEYS)
                ISLogger.Write("DEBUG: VIRTUAL KEY {0} SCAN {1}", key, keyboardStruct.scanCode);

            ISInputCode translatedCode;
            if (code == WM_KEYDOWN || code == WM_SYSKEYDOWN)
                translatedCode = ISInputCode.IS_KEYDOWN;
            else
                translatedCode = ISInputCode.IS_KEYUP;

            ISInputData translatedData = new ISInputData(translatedCode, (short)keyboardStruct.vkCode, (short)keyboardStruct.scanCode);
            if (translatedData.Code != ISInputCode.IS_UNKNOWN)
                InputReceived?.Invoke(this, translatedData);
        }

        private void CheckForHotkey(int code)
        {
            WindowsVirtualKey key = (WindowsVirtualKey)keyboardData.vkCode;
            if (code == WM_KEYDOWN || code == WM_SYSKEYDOWN)
            {
                if (key == WindowsVirtualKey.LeftMenu || key == WindowsVirtualKey.RightMenu)
                    currentActiveModifiers |= InputshareLib.Input.Hotkeys.HotkeyModifiers.Alt;
                else if (key == WindowsVirtualKey.LeftControl || key == WindowsVirtualKey.RightControl)
                    currentActiveModifiers |= InputshareLib.Input.Hotkeys.HotkeyModifiers.Ctrl;
                else if (key == WindowsVirtualKey.LeftShift || key == WindowsVirtualKey.RightShift)
                    currentActiveModifiers |= InputshareLib.Input.Hotkeys.HotkeyModifiers.Shift;
                else if (key == WindowsVirtualKey.LeftWindows || key == WindowsVirtualKey.RightWindows)
                    currentActiveModifiers |= InputshareLib.Input.Hotkeys.HotkeyModifiers.Windows;
                else
                {
                    for (int i = 0; i < hotkeys.Count; i++)
                    {
                        if (currentActiveModifiers == hotkeys[i].Modifiers)
                        {
                            if (key == hotkeys[i].Key)
                            {
                                if (hotkeys[i].GetType() == typeof(FunctionHotkey))
                                    OnFunctionHotkeyPressed((hotkeys[i] as FunctionHotkey).Function);
                                else if (hotkeys[i].GetType() == typeof(ClientHotkey))
                                    OnClientHotkeyPressed((hotkeys[i] as ClientHotkey).TargetClient);
                            }
                        }
                    }
                }
            }
            else
            {
                if (key == WindowsVirtualKey.LeftMenu || key == WindowsVirtualKey.RightMenu)
                    currentActiveModifiers &= InputshareLib.Input.Hotkeys.HotkeyModifiers.Alt;
                else if (key == WindowsVirtualKey.LeftControl || key == WindowsVirtualKey.RightControl)
                    currentActiveModifiers &= InputshareLib.Input.Hotkeys.HotkeyModifiers.Ctrl;
                else if (key == WindowsVirtualKey.LeftShift || key == WindowsVirtualKey.RightShift)
                    currentActiveModifiers &= InputshareLib.Input.Hotkeys.HotkeyModifiers.Shift;
                else if (key == WindowsVirtualKey.LeftWindows || key == WindowsVirtualKey.RightWindows)
                    currentActiveModifiers &= InputshareLib.Input.Hotkeys.HotkeyModifiers.Windows;
            }
        }

        

        #endregion

        #region consts



        public WindowsInputManager()
        {
        }

        #endregion

        #region structs
        [StructLayout(LayoutKind.Sequential)]
        public class MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public UIntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public class KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public uint dwExtraInfo;
        }
        #endregion
    }
}
