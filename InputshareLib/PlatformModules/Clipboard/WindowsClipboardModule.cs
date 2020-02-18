using InputshareLib.Clipboard;
using InputshareLib.PlatformModules.Windows;
using InputshareLib.PlatformModules.Windows.Clipboard;
using InputshareLib.PlatformModules.Windows.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using static InputshareLib.PlatformModules.Windows.Native.User32;
using static InputshareLib.PlatformModules.Windows.Native.Ole32;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using IDataObject = InputshareLib.PlatformModules.Windows.Native.Interfaces.IDataObject;

namespace InputshareLib.PlatformModules.Clipboard
{
    public class WindowsClipboardModule : ClipboardModuleBase
    {
        public override event EventHandler<ClipboardData> ClipboardChanged;

        private WinMessageWindow _window;

        public override Task SetClipboardAsync(ClipboardData cbData)
        {
            return Task.CompletedTask;
        }

        protected override async Task OnStart()
        {
            _window = await WinMessageWindow.CreateWindowAsync("InputshareCbWnd");
            InstallClipboardMontior(_window );
        }

        private void InstallClipboardMontior(WinMessageWindow window)
        {
            window.InvokeAction(() => {
                AddClipboardFormatListener(window.Handle);
            });

            window.MessageRecevied += OnWindowMessageRecieved;
        }

        private void OnWindowMessageRecieved(object sender, Win32Message e)
        {
            if(e.message == (int)Win32MessageCode.WM_CLIPBOARDUPDATE)
            {
                try
                {
                    Logger.Write("Clipboard updated!");
                    ReadClipboard();
                }
                catch(Win32Exception ex)
                {
                    Logger.Write("Failed to read clipboard: " + ex.Message + "\n " + ex.StackTrace);
                }
                
            }
        }

        private ClipboardData ReadClipboard()
        {
            var dataObject = OpenClipboard();
  
            dataObject.EnumFormatEtc(DATADIR.DATADIR_GET, out var enumer);
            FORMATETC[] f = new FORMATETC[1];
            int[] g = new int[1];

            do
            {
                enumer.Next(1, f, g);
                StringBuilder sb = new StringBuilder(256);
                GetClipboardFormatName((uint)f[0].cfFormat, sb, sb.Capacity);

                if(sb.ToString() == "")
                {
                    Logger.Write(f[0].cfFormat.ToString());
                }
                else
                {
                    Logger.Write(sb.ToString());
                }

                if(f[0].cfFormat == 15)
                {

                }
                
            } while (g[0] != 0);

            return null;
        }

        private string GetClipboardText()
        {

        }

        private IDataObject OpenClipboard()
        {
            IDataObject dataObject = null;
            for (int i = 0; i < 10; i++)
            {
                if (OleGetClipboard(out dataObject) == IntPtr.Zero)
                {
                    Logger.Write("Opened clipboard");
                    break;
                }


                Thread.Sleep(25);
                if (i == 9)
                    throw new Win32Exception();
            }

            return dataObject;
        }

        protected override Task OnStop()
        {
            _window.Dispose();
            return Task.CompletedTask;
        }
    }
}
