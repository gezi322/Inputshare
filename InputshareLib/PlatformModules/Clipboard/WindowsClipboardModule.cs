using InputshareLib.Clipboard;
using InputshareLib.PlatformModules.Windows;
using InputshareLib.PlatformModules.Windows.Clipboard;
using InputshareLib.PlatformModules.Windows.Native;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using static InputshareLib.PlatformModules.Windows.Native.User32;
using static InputshareLib.PlatformModules.Windows.Native.Kernel32;
using System.Runtime.InteropServices;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.InteropServices.ComTypes;

namespace InputshareLib.PlatformModules.Clipboard
{
    /// <summary>
    /// Monitors and sets the clipboard for windows
    /// </summary>
    public class WindowsClipboardModule : ClipboardModuleBase
    {
        public override event EventHandler<ClipboardData> ClipboardChanged;

        private Win32MessageForm _window;

        /// <summary>
        /// Sets the clipboard data
        /// </summary>
        /// <param name="cbData"></param>
        /// <returns></returns>
        public override Task SetClipboardAsync(ClipboardData cbData)
        {
            _window.InvokeAction(async () => {

                try
                {
                    ClipboardDataObject obj = await ClipboardDataObject.CreateAsync(cbData);

                    //When the object is pasted by another program, place a new instance
                    //of the dataobject back on the clipboard to create multiple instances
                    //of the file streams
                    obj.Pasted += async(object o, ClipboardDataObject obj) =>
                    {
                        await SetClipboardAsync(obj.InnerData);
                    };

                    Ole32.OleFlushClipboard();
                    IntPtr ret = Ole32.OleSetClipboard(obj);
                    SetClipboardData((uint)WinClipboardDataFormat.InputshareFormat, IntPtr.Zero);
                }
                catch (Exception ex)
                {
                    Logger.Write("Failed to set clipboard data: " + ex.Message + "\n" + ex.StackTrace);
                }
            });

            return Task.CompletedTask;
        }

        protected override async Task OnStart()
        {
            _window = await Win32MessageForm.CreateAsync();
            InstallClipboardMontior(_window);
        }

        private void InstallClipboardMontior(Win32MessageForm window)
        {
            window.InvokeAction(() => {
                AddClipboardFormatListener(window.Handle);
            });

            window.MessageRecevied += OnWindowMessageRecieved;
        }

        private async void OnWindowMessageRecieved(object sender, Win32Message e)
        {
            if(e.message == (int)Win32MessageCode.WM_CLIPBOARDUPDATE)
            {
                try
                {
                    var obj = await OpenOleClipboardAsync().ConfigureAwait(true);
                    if (obj == null)
                    {
                        Logger.Write("Failed to open OLE clipboard");
                        return;
                    }

                    FORMATETC format = new FORMATETC
                    {
                        cfFormat = WinClipboardDataFormat.InputshareFormat,
                        dwAspect = DVASPECT.DVASPECT_CONTENT,
                        lindex = -1,
                        ptd = IntPtr.Zero,
                        tymed = TYMED.TYMED_NULL
                    };

                    if(obj.QueryGetData(ref format) == IntPtr.Zero)
                        return;
                    
                    await OpenClipboardAsync();
                    var data = ReadClipboard();
                    CloseClipboard();
                    ClipboardChanged?.Invoke(this, data);
                }
                catch(Exception ex)
                {
                    Logger.Write("Failed to read clipboard: " + ex.Message + "\n " + ex.StackTrace);
                }
                finally
                {
                    CloseClipboard();
                }
            }
        }

        private async Task<Windows.Native.Interfaces.IDataObject> OpenOleClipboardAsync()
        {
            for (int i = 0; i < 10; i++)
            {
                IntPtr ret = Ole32.OleGetClipboard(out var obj);

                if (ret == IntPtr.Zero)
                    return obj;

                await Task.Delay(50);
            }

            return null;
        }

        /// <summary>
        /// Attempts to open the clipboard
        /// </summary>
        /// <returns></returns>
        private async Task OpenClipboardAsync()
        {
            for (int i = 0; i < 10; i++)
            {
                if (OpenClipboard(_window.Handle))
                    return;

                await Task.Delay(50);
            }

            throw new Win32Exception();
        }

        /// <summary>
        /// Opens the clipboard and copies the data into a managed format
        /// </summary>
        /// <returns></returns>
        private ClipboardData ReadClipboard()
        {
            ClipboardData cbData = new ClipboardData();

            if (IsClipboardFormatAvailable(WinClipboardDataFormat.CF_HDROP))
                ReadFileDrop(cbData);

            if (IsClipboardFormatAvailable(WinClipboardDataFormat.CF_UNICODETEXT))
                ReadText(cbData);

            if (IsClipboardFormatAvailable(WinClipboardDataFormat.CF_BITMAP))
                ReadBitmap(cbData);

            return cbData;
        }

        /// <summary>
        /// Reads text stored on the clipboard
        /// </summary>
        /// <param name="cbData"></param>
        private void ReadText(ClipboardData cbData)
        {
            IntPtr ptr = GetClipboardData(WinClipboardDataFormat.CF_UNICODETEXT);

            if (ptr == default)
                throw new Win32Exception();

            var sizePtr = GlobalSize(ptr);

            byte[] buffer = new byte[0];

            if(IntPtr.Size == 8)
                buffer = new byte[sizePtr.ToUInt64()];
            else
                buffer = new byte[sizePtr.ToUInt32()];

            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            string str = Encoding.Unicode.GetString(buffer);
            cbData.SetText(str);
        }

        /// <summary>
        /// Gets a handle to a bitmap and converts to a byte array
        /// and stores it in the given clipboarddata object
        /// </summary>
        /// <param name="cbData"></param>
        private void ReadBitmap(ClipboardData cbData)
        {
            IntPtr gdiBitmap = GetClipboardData(WinClipboardDataFormat.CF_BITMAP);

            if (gdiBitmap == default)
                throw new Win32Exception();

            Bitmap bmp = Bitmap.FromHbitmap(gdiBitmap);

            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                cbData.SetBitmap(ms.ToArray());
            }
        }

        /// <summary>
        /// Reads a list of files stored on the clipboard
        /// </summary>
        /// <param name="cbData"></param>
        private void ReadFileDrop(ClipboardData cbData)
        {
            IntPtr ptr = GetClipboardData(WinClipboardDataFormat.CF_HDROP);

            if (ptr == default)
                throw new Win32Exception();

            var sizePtr = GlobalSize(ptr);
            byte[] buffer = new byte[sizePtr.ToUInt64()];
            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            string str = Encoding.Unicode.GetString(buffer);
            //The data returned by GetClipboardData(CF_HDROP) is a DROPFILES struct which
            //if followed by an unmanaged string which is a list of files. 

            //Remove the header
            str = str.Substring(10);
            //Remove the double null terminator at the end of the string
            str = str.Substring(0, str.Length - 2);
            //Split the string into seperate files
            var result = str.Split('\0');

            cbData.SetLocalFiles(result);
        }

        protected override Task OnStop()
        {
            _window.Dispose();
            return Task.CompletedTask;
        }
    }
}
