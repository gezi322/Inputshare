using InputshareLib.Input;
using InputshareLib.Input.Keys;
using InputshareLib.Linux;
using System;
using static InputshareLib.Linux.Native.LibX11;


namespace InputshareLib.PlatformModules.Output
{
    public class LinuxOutputManager : OutputManagerBase
    {
        private SharedXConnection xConnection;

        public LinuxOutputManager(SharedXConnection xCon)
        {
            xConnection = xCon;
        }

        public override void ResetKeyStates()
        {

            //Fix this - causes segmentation fault
            /*
            foreach(var key in (LinuxKeyCode[])Enum.GetValues(typeof(LinuxKeyCode)))
            {
                if ((uint)key == 0)
                    continue;

                XTestFakeKeyEvent(xConnection.XDisplay, (uint)key, false, 0);
            }*/
        }

        public override void Send(ISInputData input)
        {

            if (input.Code == ISInputCode.IS_MOUSEMOVERELATIVE)
                XWarpPointer(xConnection.XDisplay, IntPtr.Zero, IntPtr.Zero, 0, 0, 0, 0, input.Param1, input.Param2);
            else if (input.Code == ISInputCode.IS_MOUSELDOWN)
                XTestFakeButtonEvent(xConnection.XDisplay, X11_LEFTBUTTON, true, 0);
            else if (input.Code == ISInputCode.IS_MOUSELUP)
                XTestFakeButtonEvent(xConnection.XDisplay, X11_LEFTBUTTON, false, 0);
            else if (input.Code == ISInputCode.IS_MOUSERDOWN)
                XTestFakeButtonEvent(xConnection.XDisplay, X11_RIGHTBUTTON, true, 0);
            else if (input.Code == ISInputCode.IS_MOUSERUP)
                XTestFakeButtonEvent(xConnection.XDisplay, X11_RIGHTBUTTON, false, 0);
            else if (input.Code == ISInputCode.IS_MOUSEMDOWN)
                XTestFakeButtonEvent(xConnection.XDisplay, X11_MIDDLEBUTTON, true, 0);
            else if (input.Code == ISInputCode.IS_MOUSEMUP)
                XTestFakeButtonEvent(xConnection.XDisplay, X11_MIDDLEBUTTON, false, 0);
            else if (input.Code == ISInputCode.IS_MOUSEYSCROLL)
            {
                //Param1 contains the mouse direction, 120 = up; -120 = down
                if (input.Param1 > 0)
                {
                    XTestFakeButtonEvent(xConnection.XDisplay, X11_SCROLLDOWN, true, 0);
                    XTestFakeButtonEvent(xConnection.XDisplay, X11_SCROLLDOWN, false, 0);
                }
                else
                {
                    XTestFakeButtonEvent(xConnection.XDisplay, X11_SCROLLUP, true, 0);
                    XTestFakeButtonEvent(xConnection.XDisplay, X11_SCROLLUP, false, 0);
                }
            }
            else if (input.Code == ISInputCode.IS_MOUSEXSCROLL)
            {
                //todo
            }
            else if (input.Code == ISInputCode.IS_MOUSEXDOWN)
            {
                //first param is the ID of the button. 4 = forward, 5 = back
                if (input.Param1 == 4)
                    XTestFakeButtonEvent(xConnection.XDisplay, X11_XBUTTONFORWARD, true, 0);
                else
                    XTestFakeButtonEvent(xConnection.XDisplay, X11_XBUTTONBACK, true, 0);
            }
            else if (input.Code == ISInputCode.IS_MOUSEXUP)
            {
                if (input.Param1 == 4)
                    XTestFakeButtonEvent(xConnection.XDisplay, X11_XBUTTONFORWARD, false, 0);
                else
                    XTestFakeButtonEvent(xConnection.XDisplay, X11_XBUTTONBACK, false, 0);
            }
            else if (input.Code == ISInputCode.IS_KEYDOWN)
            {
                try
                {
                    uint key = ConvertKey((WindowsVirtualKey)input.Param1);

                    if (key < 1)
                        throw new Exception("Could not translate key " + (WindowsVirtualKey)input.Param1);

                    XTestFakeKeyEvent(xConnection.XDisplay, key, true, 0);
                }
                catch (Exception ex)
                {
                    ISLogger.Write("Failed to send key {0}: {1}", (WindowsVirtualKey)input.Param1, ex.Message);
                }
            }
            else if (input.Code == ISInputCode.IS_KEYUP)
            {
                try
                {
                    uint key = ConvertKey((WindowsVirtualKey)input.Param1);

                    if (key < 1)
                        throw new Exception("Could not translate key " + (WindowsVirtualKey)input.Param1);

                    XTestFakeKeyEvent(xConnection.XDisplay, key, false, 0);
                }
                catch (Exception ex)
                {
                    ISLogger.Write("Failed to send key {0}: {1}", (WindowsVirtualKey)input.Param1, ex.Message);
                }
            }


            XFlush(xConnection.XDisplay);
        }

        private uint ConvertKey(WindowsVirtualKey winKey)
        {
            //TODO - Some keys can only be implemented via keysyms
            return (uint)KeyTranslator.WindowsToLinux(winKey);
        }

        protected override void OnStart()
        {

        }

        protected override void OnStop()
        {

        }
    }
}
