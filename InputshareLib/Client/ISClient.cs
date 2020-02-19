﻿using InputshareLib.Net.Client;
using InputshareLib.Net.RFS;
using InputshareLib.Net.RFS.Client;
using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;
using InputshareLib.Server.Display;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Client
{
    public sealed class ISClient
    {
        private InputModuleBase inputMod;
        private ClipboardModuleBase cbMod;
        private OutputModuleBase outMod;
        private ClientSocket soc;
        private bool _input;

        private bool cLeft;
        private bool cRight;
        private bool cTop;
        private bool cBottom;

        public ISClient()
        {

        }

        private RFSController _fileController = new RFSController();

        public async Task StartAsync()
        {
            inputMod = new WindowsInputModule();
            await inputMod.StartAsync();
            cbMod = new WindowsClipboardModule();
            cbMod.ClipboardChanged += CbMod_ClipboardChanged;
            await cbMod.StartAsync();
            outMod = new WindowsOutputModule();
            inputMod.SideHit += InputMod_SideHit;
            inputMod.DisplayBoundsUpdated += InputMod_DisplayBoundsUpdated;
            await outMod.StartAsync();
            soc = new ClientSocket(_fileController);
            soc.ClipboardDataReceived += Soc_ClipboardDataReceived;
            soc.Disconnected += Soc_Disconnected;
            soc.ScreenshotRequested += Soc_ScreenshotRequested;
            soc.InputClientChanged += Soc_InputClientChanged;
            soc.InputReceived += Soc_InputReceived;
            soc.SideStateChanged += Soc_SideStateChanged1;
            await soc.ConnectAsync(new ClientConnectArgs(new IPEndPoint(IPAddress.Parse("192.168.0.17"), 1234), Environment.MachineName, Guid.NewGuid(), inputMod.VirtualDisplayBounds));

        }

        private async void CbMod_ClipboardChanged(object sender, Clipboard.ClipboardData cbData)
        {
            if (cbData.IsTypeAvailable(Clipboard.ClipboardDataType.HostFileGroup))
            {
                string[] files = cbData.GetLocalFiles();
                var group = _fileController.HostFiles(files);
                cbData.SetRemoteFiles(group);
            }
           
            await soc.SendClipboardDataAsync(cbData);
        }

        private async void Soc_ClipboardDataReceived(object sender, Clipboard.ClipboardData e)
        {
            Logger.Write("Received clipboard data! " + e.AvailableTypes.Length);

            if (e.IsTypeAvailable(Clipboard.ClipboardDataType.RemoteFileGroup))
            {
                var group = e.GetRemoteFiles();

                RFSClientFileGroup fg = new RFSClientFileGroup(group.GroupId, group.Files, soc);
                e.SetRemoteFiles(fg);
            }

            await cbMod.SetClipboardAsync(e);
        }

        private void Soc_SideStateChanged1(object sender, ClientSidesChangedArgs e)
        {
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
            _input = e;
            inputMod.SetMouseHidden(!e);
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
            Logger.Write(e.StackTrace);
            soc.Dispose();
        }

        private async void InputMod_SideHit(object sender, SideHitArgs e)
        {
            if(soc.State == ClientSocketState.Connected && IsDisplayAtSide(e.Side) &&_input)
             {
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
