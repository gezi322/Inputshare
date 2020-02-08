using InputshareLib.Input;
using InputshareLib.PlatformModules.Windows.Native;
using System;
using System.Collections.Generic;
using System.Text;
using static InputshareLib.PlatformModules.Windows.Native.User32;

namespace InputshareLib.PlatformModules.Windows
{
    internal static class WindowsInputTranslator
    {
        internal static InputData WindowsToGeneric(Win32MessageCode wmCode, ref MSLLHOOKSTRUCT mouseStruct)
        {
            switch (wmCode)
            {
                case Win32MessageCode.WM_MOUSEMOVE:
                    return new InputData(InputCode.MouseMoveRelative, (short)mouseStruct.pt.X, (short)mouseStruct.pt.Y);
                case Win32MessageCode.WM_LBUTTONDOWN:
                    return new InputData(InputCode.Mouse1Down, 0, 0);
                case Win32MessageCode.WM_LBUTTONUP:
                    return new InputData(InputCode.Mouse1Up, 0, 0);
                case Win32MessageCode.WM_RBUTTONDOWN:
                    return new InputData(InputCode.Mouse2Down, 0, 0);
                case Win32MessageCode.WM_RBUTTONUP:
                    return new InputData(InputCode.Mouse2Up, 0, 0);
                case Win32MessageCode.WM_MBUTTONDOWN:
                    return new InputData(InputCode.MouseMDown, 0, 0);
                case Win32MessageCode.WM_MBUTTONUP:
                    return new InputData(InputCode.MouseMUp, 0, 0);
                case Win32MessageCode.WM_MOUSEWHEEL:
                    return new InputData(InputCode.MouseYScroll, unchecked((short)((long)mouseStruct.mouseData >> 16)), 0);
                case Win32MessageCode.WM_XBUTTONDOWN:
                    return new InputData(InputCode.MouseXDown, unchecked((short)((long)mouseStruct.mouseData >> 16)), 0);
                case Win32MessageCode.WM_XBUTTONUP:
                    return new InputData(InputCode.MouseXUp, unchecked((short)((long)mouseStruct.mouseData >> 16)), 0);
                default:
                    return new InputData(0, 0, 0);
            }
        }

        internal static InputData WindowsToGeneric(Win32MessageCode wmCode, ref KBDLLHOOKSTRUCT kbStruct)
        {
            switch (wmCode)
            {
                case Win32MessageCode.WM_KEYDOWN:
                case Win32MessageCode.WM_SYSKEYDOWN:
                    if (kbStruct.scanCode == 0 || kbStruct.scanCode == 91)
                        return new InputData(InputCode.KeyDownVKey, (short)kbStruct.vkCode, 0);
                    else
                        return new InputData(InputCode.KeyDownScan, (short)kbStruct.scanCode, 0);
                case Win32MessageCode.WM_KEYUP:
                case Win32MessageCode.WM_SYSKEYUP:
                    if (kbStruct.scanCode == 0 || kbStruct.scanCode == 91)
                        return new InputData(InputCode.KeyUpVKey, (short)kbStruct.vkCode, 0);
                    else
                        return new InputData(InputCode.keyUpScan, (short)kbStruct.scanCode, 0);
                default:
                    return new InputData(0, 0, 0);
            }
        }

        internal static InputData WindowsMouseMoveToGeneric(ref User32.MSLLHOOKSTRUCT mouseStruct, int oldX, int oldY)
        {
            return new InputData(InputCode.MouseMoveRelative, (short)(mouseStruct.pt.X - oldX), (short)(mouseStruct.pt.Y - oldY));
        }
    }
}
