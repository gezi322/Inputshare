using InputshareLib.Net.Client;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Client
{
    public sealed class ISClient
    {
        private InputModuleBase inputMod;
        private OutputModuleBase outMod;
        private ClientSocket soc;
        private bool _input;
        private bool _f = true;

        private bool cLeft;
        private bool cRight;
        private bool cTop;
        private bool cBottom;

        public ISClient()
        {

        }

        public async Task StartAsync()
        {
            inputMod = new WindowsInputModule();
            await inputMod.StartAsync();
            outMod = new WindowsOutputModule();
            inputMod.SideHit += InputMod_SideHit;
            inputMod.DisplayBoundsUpdated += InputMod_DisplayBoundsUpdated;
            await outMod.StartAsync();
            soc = new ClientSocket();
            soc.Disconnected += Soc_Disconnected;
            soc.ScreenshotRequested += Soc_ScreenshotRequested;
            soc.InputClientChanged += Soc_InputClientChanged;
            soc.InputReceived += Soc_InputReceived;
            soc.SideStateChanged += Soc_SideStateChanged1;
            await soc.ConnectAsync(new ClientConnectArgs(new IPEndPoint(IPAddress.Parse("192.168.0.17"), 1234), Environment.MachineName, Guid.NewGuid(), inputMod.VirtualDisplayBounds));

        }

        private void Soc_SideStateChanged1(object sender, ClientSidesChangedArgs e)
        {
            Logger.Write("???");
            cLeft = e.Left;
            cRight = e.Right;
            cTop = e.Top;
            cBottom = e.Bottom;
            Logger.Write($"Active sides: {cLeft}, {cRight}, {cTop}, {cBottom}");
        }

        private void Soc_ScreenshotRequested(object sender, ScreenshotRequestArgs e)
        {
            //Create a new bitmap.
            var bmpScreenshot = new Bitmap(1920,
                                           1080,
                                           PixelFormat.Format32bppArgb);

            // Create a graphics object from the bitmap.
            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

            // Take the screenshot from the upper left corner to the right bottom corner.
            gfxScreenshot.CopyFromScreen(0,
                                        0,
                                        0,
                                        0,
                                        new Size(1920,1080),
                                        CopyPixelOperation.SourceCopy);

            byte[] d = null;
            using (MemoryStream ms = new MemoryStream())
            {
                bmpScreenshot.Save(ms, ImageFormat.Png);
                d = ms.ToArray();
            }

            Logger.Write("Size = " + d.Length);
            e.Data = d;

        }

        private void Soc_InputClientChanged(object sender, bool e)
        {
            if(_input != e || _f)
            {
                _f = false;
                _input = e;
                inputMod.SetMouseHidden(!e);
            }
            
        }

        private async void InputMod_DisplayBoundsUpdated(object sender, System.Drawing.Rectangle e)
        {
            await soc.SendDisplayUpdateAsync(e);
        }

        private void Soc_InputReceived(object sender, Input.InputData e)
        {
            outMod.SimulateInput(ref e);
        }

        private void Soc_Disconnected(object sender, Exception e)
        {
            Logger.Write("Disconnected: " + e.Message);
            soc.Dispose();
        }

        private async void InputMod_SideHit(object sender, SideHitArgs e)
        {
            if(soc.State == ClientSocketState.Connected && IsDisplayAtSide(e.Side) &&_input)
             {
                _input = false;
                await soc.SendSideHitAsync(e.Side, e.PosX, e.PosY);
            }
        }

        bool IsDisplayAtSide(Side side)
        {
            switch (side)
            {
                case Side.Top:
                    return cTop;
                case Side.Bottom:
                    return cBottom;
                case Side.Left:
                    return cLeft;
                case Side.Right:
                    return cRight;
                default:
                    return false;
            }
        }
    }
}
