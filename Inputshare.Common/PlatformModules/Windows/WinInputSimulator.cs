using Inputshare.Common.Input;
using System;
using System.Collections.Generic;
using System.Text;
using static Inputshare.Common.PlatformModules.Windows.Native.User32;

namespace Inputshare.Common.PlatformModules.Windows
{
    internal static class WinInputSimulator
    {
        public static void SendInput(ref InputData input)
        {
            if (input.Code == InputCode.MouseMoveRelative)
                MoveMouseRelative(input.ParamA, input.ParamB);
            else if (input.Code == InputCode.Mouse1Down)
                MouseLDown(true);
            else if (input.Code == InputCode.Mouse1Up)
                MouseLDown(false);
            else if (input.Code == InputCode.Mouse2Down)
                MouseRDown(true);
            else if (input.Code == InputCode.Mouse2Up)
                MouseRDown(false);
            else if (input.Code == InputCode.MouseMDown)
                MouseMDown(true);
            else if (input.Code == InputCode.MouseMUp)
                MouseMDown(false);
            else if (input.Code == InputCode.KeyDownScan)
                KeyDownScan(input.ParamA, true);
            else if (input.Code == InputCode.keyUpScan)
                KeyDownScan(input.ParamA, false);
            else if (input.Code == InputCode.KeyDownVKey)
                KeyDownVirtual(input.ParamA, true);
            else if (input.Code == InputCode.KeyUpVKey)
                KeyDownVirtual(input.ParamA, false);
            else if (input.Code == InputCode.MouseYScroll)
                MouseYScroll(input.ParamA);
            else if (input.Code == InputCode.MouseXDown)
                MouseXDown(input.ParamA, true);
            else if (input.Code == InputCode.MouseXUp)
                MouseXDown(input.ParamA, false);
            else if (input.Code == InputCode.MouseMoveAbsolute)
                SetCursorPos(input.ParamA, input.ParamB);

        }

        private static void KeyDownVirtual(short vKey, bool down)
        {
            WinInputStruct kbIn;
            kbIn.type = 1; //type keyboard
            uint flags;
            if (down)
                flags = (uint)KeyEventF.KeyDown;
            else
                flags = (uint)KeyEventF.KeyUp;

            kbIn.u = new InputUnion
            {
                ki = new KeyboardInput
                {
                    wVk = (ushort)vKey,
                    wScan = 0,
                    dwFlags = flags,
                    dwExtraInfo = IntPtr.Zero,
                    time = 0,
                }
            };

            Native.User32.SendInput(1, new WinInputStruct[1] { kbIn }, InputSize);
        }

        private static void MouseXDown(short button, bool down)
        {
            WinInputStruct mouseIn;
            mouseIn.type = 0; //type mouse

            uint flags;
            if (down)
                flags = MOUSEEVENTF_XDOWN;
            else
                flags = MOUSEEVENTF_XUP;

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = (uint)button,
                    time = 0,
                    dx = 0,
                    dy = 0,
                    dwFlags = flags
                }
            };

            Native.User32.SendInput(1, new WinInputStruct[1] { mouseIn }, InputSize);
        }

        private static void KeyDownScan(short scan, bool down)
        {
            WinInputStruct kbIn;
            kbIn.type = 1; //type keyboarrd
            uint flags;
            if (down)
                flags = (uint)(KeyEventF.Scancode | KeyEventF.KeyDown);
            else
                flags = (uint)(KeyEventF.Scancode | KeyEventF.KeyUp);

            kbIn.u = new InputUnion
            {
                ki = new KeyboardInput
                {
                    wVk = 0,
                    wScan = (ushort)scan,
                    dwFlags = flags,
                    dwExtraInfo = IntPtr.Zero,
                    time = 0,
                }
            };

            Native.User32.SendInput(1, new WinInputStruct[1] { kbIn }, InputSize);
        }

        private static void MoveMouseAbsolute(short x, short y)
        {
            WinInputStruct mouseIn;
            mouseIn.type = 0; //type mouse

            var pt = ConvertScreenPointToAbsolutePoint((uint)x, (uint)y);
            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = 0,
                    time = 0,
                    dx = pt.X,
                    dy = pt.Y,
                    //dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_MOVE_NOCOALESCE
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                }
            };

            Native.User32.SendInput(1, new WinInputStruct[1] { mouseIn }, InputSize);
        }

        private static void MoveMouseRelative(short x, short y)
        {
            WinInputStruct mouseIn;
            mouseIn.type = 0; //type mouse

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = 0,
                    time = 0,
                    dx = x,
                    dy = y,
                    //dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_MOVE_NOCOALESCE
                    dwFlags = MOUSEEVENTF_MOVE
                }
            };

            Native.User32.SendInput(1, new WinInputStruct[1] { mouseIn }, InputSize);
        }

        private static void MouseYScroll(short dir)
        {
            WinInputStruct mouseIn;
            mouseIn.type = 0; //type mouse

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = (uint)dir,
                    time = 0,
                    dx = 0,
                    dy = 0,
                    dwFlags = MOUSEEVENTF_WHEEL
                }
            };

            Native.User32.SendInput(1, new WinInputStruct[1] { mouseIn }, InputSize);
        }

        private static void MouseLDown(bool down)
        {
            WinInputStruct mouseIn;
            mouseIn.type = 0; //type mouse
            uint flags;
            if (down)
                flags = MOUSEEVENTF_LEFTDOWN;
            else
                flags = MOUSEEVENTF_LEFTUP;

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = 0,
                    time = 0,
                    dx = 0,
                    dy = 0,
                    dwFlags = flags
                }
            };

            Native.User32.SendInput(1, new WinInputStruct[1] { mouseIn }, InputSize);
        }

        private static void MouseRDown(bool down)
        {
            WinInputStruct mouseIn;
            mouseIn.type = 0; //type mouse
            uint flags;
            if (down)
                flags = MOUSEEVENTF_RIGHTDOWN;
            else
                flags = MOUSEEVENTF_RIGHTUP;

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = 0,
                    time = 0,
                    dx = 0,
                    dy = 0,
                    dwFlags = flags
                }
            };

            Native.User32.SendInput(1, new WinInputStruct[1] { mouseIn }, InputSize);
        }

        private static void MouseMDown(bool down)
        {
            WinInputStruct mouseIn;
            mouseIn.type = 0; //type mouse
            uint flags;
            if (down)
                flags = MOUSEEVENTF_MIDDLEDOWN;
            else
                flags = MOUSEEVENTF_MIDDLEUP;

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = 0,
                    time = 0,
                    dx = 0,
                    dy = 0,
                    dwFlags = flags
                }
            };

            Native.User32.SendInput(1, new WinInputStruct[1] { mouseIn }, InputSize);
        }

        static POINT ConvertScreenPointToAbsolutePoint(uint x, uint y)
        {
            // Get current desktop maximum screen resolution-1
            int screenMaxWidth = GetSystemMetrics(0) - 1;
            int screenMaxHeight = GetSystemMetrics(1) - 1;

            double convertedPointX = (x * (65535.0f / screenMaxWidth));
            double convertedPointY = (y * (65535.0f / screenMaxHeight));

            return new POINT
            {
                X = (int)convertedPointX,
                Y = (int)convertedPointY
            };
        }
    }
}
