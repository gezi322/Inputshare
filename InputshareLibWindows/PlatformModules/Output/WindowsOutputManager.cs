using InputshareLib;
using InputshareLib.Input;
using InputshareLib.PlatformModules.Output;
using InputshareLibWindows.Native;
using System;
using System.Runtime.InteropServices;
using static InputshareLibWindows.Native.User32;
using Input = InputshareLibWindows.Native.User32.Input;
namespace InputshareLibWindows.Output
{
    public class WindowsOutputManager : OutputManagerBase
    {
        public override void Send(ISInputData input)
        {
            ISInputCode c = input.Code;
            switch (c)
            {
                case ISInputCode.IS_MOUSEMOVERELATIVE:
                    MoveMouseRelative(input.Param1, input.Param2);
                    break;
                case ISInputCode.IS_KEYDOWN:
                    {
                        short useKey = CheckKey(input, out bool useScan);

                        if (useScan)
                            KeyDownScan(useKey, true);
                        else
                            KeyDownVirtual(useKey, true);

                        break;
                    }
                case ISInputCode.IS_KEYUP:
                    {
                        short useKey = CheckKey(input, out bool useScan);

                        if (useScan)
                            KeyDownScan(useKey, false);
                        else
                            KeyDownVirtual(useKey, false);

                        break;
                    }
                case ISInputCode.IS_MOUSELDOWN:
                    MouseLDown(true);
                    break;
                case ISInputCode.IS_MOUSELUP:
                    MouseLDown(false);
                    break;
                case ISInputCode.IS_MOUSERDOWN:
                    MouseRDown(true);
                    break;
                case ISInputCode.IS_MOUSERUP:
                    MouseRDown(false);
                    break;
                case ISInputCode.IS_MOUSEMDOWN:
                    MouseMDown(true);
                    break;
                case ISInputCode.IS_MOUSEMUP:
                    MouseMDown(false);
                    break;
                case ISInputCode.IS_MOUSEYSCROLL:
                    MouseYScroll(input.Param1);
                    break;
                case ISInputCode.IS_MOUSEXDOWN:
                    MouseXDown(input.Param1, true);
                    break;
                case ISInputCode.IS_MOUSEXUP:
                    MouseXDown(input.Param1, false);
                    break;
                case ISInputCode.IS_RELEASEALL:
                    ResetKeyStates();
                    break;
                case ISInputCode.IS_MOUSEMOVEABSOLUTE:
                    MoveMouseAbs(input.Param1, input.Param2);
                    break;
            }
        }
        private void MoveMouseAbs(short x, short y)
        {
            User32.Input mouseIn;
            mouseIn.type = 0; //type mouse

            POINT pt = ConvertScreenPointToAbsolutePoint((uint)x, (uint)y);

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = 0,
                    time = 0,
                    dx = pt.X,
                    dy = pt.Y,
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                }
            };

            SendInput(1, new User32.Input[1] { mouseIn }, InputSize);
        }


        /// <summary>
        /// We want to use scan codes instead of virtual codes as they are inserted lower into the stack,
        /// but some virtual keys cannot be mapped into scan codes so we need to decide whether to use 
        /// the scan or virtual key
        /// </summary>
        /// <param name="input"></param>
        /// <param name="useScan"></param>
        /// <returns></returns>
        private short CheckKey(ISInputData input, out bool useScan)
        {
            useScan = false;

            if (input.Param2 == 0 || input.Param1 == 91)
                return input.Param1;

            uint mappedScanb = MapVirtualKeyA((uint)input.Param1, MAPVKTYPE.MAPVK_VK_TO_VSC);
            if (mappedScanb != input.Param2)
            {
                ISLogger.Write("Invalid key virtual key:{0} scan code:{1} mapped: {2}", input.Param1, input.Param2, mappedScanb);
                return input.Param2;
            }

            useScan = true;
            return input.Param2;
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

        private void KeyDownVirtual(short vKey, bool down)
        {
            User32.Input kbIn;
            kbIn.type = 1; //type keyboarrd
            uint flags;
            if (down)
                flags = 0;
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

            SendInput(1, new User32.Input[1] { kbIn }, InputSize);
        }

        private void MouseXDown(short button, bool down)
        {
            User32.Input mouseIn;
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

            SendInput(1, new User32.Input[1] { mouseIn }, InputSize);
        }

        private void KeyDownScan(short scan, bool down)
        {
            User32.Input kbIn;
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

            SendInput(1, new User32.Input[1] { kbIn }, InputSize);
        }

        private void MoveMouseRelative(short x, short y)
        {
            User32.Input mouseIn;
            mouseIn.type = 0; //type mouse

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = 0,
                    time = 0,
                    dx = x,
                    dy = y,
                    dwFlags = MOUSEEVENTF_MOVE
                }
            };

            if (SendInput(1, new User32.Input[1] { mouseIn }, InputSize) != 1)
            {
                ISLogger.Write("Sendinput failed " + Marshal.GetLastWin32Error());
            }
        }

        private void MouseYScroll(short dir)
        {
            User32.Input mouseIn;
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

            SendInput(1, new User32.Input[1] { mouseIn }, InputSize);
        }

        private void MouseLDown(bool down)
        {
            User32.Input mouseIn;
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

            SendInput(1, new User32.Input[1] { mouseIn }, InputSize);
        }

        private void MouseRDown(bool down)
        {
            User32.Input mouseIn;
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

            SendInput(1, new User32.Input[1] { mouseIn }, InputSize);
        }

        private void MouseMDown(bool down)
        {
            User32.Input mouseIn;
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

            SendInput(1, new User32.Input[1] { mouseIn }, InputSize);
        }

        public override void ResetKeyStates()
        {
            for (int i = 6; i < 255; i++)
            {
                if (((1 << 15) & GetAsyncKeyState(i)) != 0)
                {
                    KeyDownVirtual((short)i, false);
                }
            }
        }

        protected override void OnStart()
        {

        }

        protected override void OnStop()
        {

        }
    }
}
