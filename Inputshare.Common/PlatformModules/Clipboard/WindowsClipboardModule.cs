using Inputshare.Common.Clipboard;
using Inputshare.Common.PlatformModules.Windows;
using Inputshare.Common.PlatformModules.Windows.Clipboard;
using Inputshare.Common.PlatformModules.Windows.Native;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using static Inputshare.Common.PlatformModules.Windows.Native.User32;
using static Inputshare.Common.PlatformModules.Windows.Native.Kernel32;
using System.Runtime.InteropServices;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.InteropServices.ComTypes;

namespace Inputshare.Common.PlatformModules.Clipboard
{
    /// <summary>
    /// Monitors and sets the clipboard for windows
    /// </summary>
    public class WindowsClipboardModule : ClipboardModuleBase
    {
        public override event EventHandler<ClipboardData> ClipboardChanged;

        private WinMessageWindow _window;

        /// <summary>
        /// Sets the clipboard data
        /// </summary>
        /// <param name="cbData"></param>
        /// <returns></returns>
        public override Task SetClipboardAsync(ClipboardData cbData)
        {
            _window.InvokeAction(() => {

                try
                {
                    ClipboardDataObject obj = ClipboardDataObject.Create(cbData);
                    //When the object is pasted by another program, place a new instance
                    //of the dataobject back on the clipboard to create multiple instances
                    //of the file streams
                    obj.FilesPasted += async(object o, ClipboardDataObject obj) =>
                    {
                        Logger.Debug("Files pasted. Resetting clipboard object");
                        await SetClipboardAsync(obj.InnerData);
                    };

                    Ole32.OleFlushClipboard();
                    IntPtr ret = Ole32.OleSetClipboard(obj);
                    SetClipboardData((uint)WinClipboardDataFormat.InputshareFormat, IntPtr.Zero);
                    Logger.Debug($"{ModuleName}: Set dataobject to clipboard (returned {ret.ToString()}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"{ModuleName}: Failed to set clipboard data: " + ex.Message + "\n" + ex.StackTrace);
                }
            });

            return Task.CompletedTask;
        }

        protected override async Task OnStart()
        {
            _window = await WinMessageWindow.CreateWindowAsync("InputshareCBMsg");
            InstallClipboardMontior(_window);
        }

        private void InstallClipboardMontior(WinMessageWindow window)
        {
            window.InvokeAction(() => {
                try
                {
                    if (!AddClipboardFormatListener(window.Handle))
                        throw new Win32Exception();
                }catch(Win32Exception ex)
                {
                    Logger.Verbose($"{ModuleName}: Added clipboard format listener: {ex.Message}");
                }
            }); 

            window.MessageRecevied += OnWindowMessageRecieved;
        }

        private void OnWindowMessageRecieved(object sender, Win32Message e)
        {
            if(e.message == (int)Win32MessageCode.WM_CLIPBOARDUPDATE)
            {
                try
                {
                    var obj = OpenOleClipboard();
                    if (obj == null)
                    {
                        Logger.Verbose("Failed to open OLE clipboard");
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
                    
                    OpenClipboard();
                    var data = ReadClipboard();
                    CloseClipboard();
                    ClipboardChanged?.Invoke(this, data);
                }
                catch(Exception ex)
                {
                    Logger.Error("Failed to read clipboard: " + ex.Message + "\n " + ex.StackTrace);
                }
                finally
                {
                    CloseClipboard();
                }
            }
        }

        private Windows.Native.Interfaces.IDataObject OpenOleClipboard()
        {
            IntPtr ret = default;

            for (int i = 0; i < 10; i++)
            {
                ret = Ole32.OleGetClipboard(out var obj);

                if (ret == IntPtr.Zero)
                    return obj;

                Thread.Sleep(25);
            }

            Logger.Verbose($"{ModuleName}: OleOpenClipboard failed (returned {ret.ToString()})");
            return null;
        }

        /// <summary>
        /// Attempts to open the clipboard
        /// </summary>
        /// <returns></returns>
        private void OpenClipboard()
        {
            for (int i = 0; i < 10; i++)
            {
                if (User32.OpenClipboard(_window.Handle))
                    return;

                Thread.Sleep(30);
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

            Logger.Debug($"{ModuleName}: Reading clipboard. available formats:");
            uint format = 0;
            while((format = EnumClipboardFormats(format)) != 0)
                Logger.Debug($"{ModuleName}: Format {WinClipboardDataFormat.GetFormatName(format)}");

            if (IsClipboardFormatAvailable(WinClipboardDataFormat.CF_HDROP))
                ReadFileDrop(cbData);

            if (IsClipboardFormatAvailable(WinClipboardDataFormat.CF_UNICODETEXT))
                ReadText(cbData);

            if (IsClipboardFormatAvailable(WinClipboardDataFormat.CF_BITMAP))
                ReadBitmap(cbData);

            Logger.Debug($"{ModuleName}: Read clipboard. Output formats: {string.Join(',', cbData.AvailableTypes)}");

            return cbData;
        }

        /// <summary>
        /// Reads text stored on the clipboard
        /// </summary>
        /// <param name="cbData"></param>
        private void ReadText(ClipboardData cbData)
        {
            try
            {
                IntPtr ptr = GetClipboardData(WinClipboardDataFormat.CF_UNICODETEXT);

                if (ptr == default)
                    throw new Win32Exception();

                var sizePtr = GlobalSize(ptr);

                byte[] buffer;

                if (IntPtr.Size == 8)
                    buffer = new byte[sizePtr.ToUInt64()];
                else
                    buffer = new byte[sizePtr.ToUInt32()];

                Marshal.Copy(ptr, buffer, 0, buffer.Length);
                string str = Encoding.Unicode.GetString(buffer);
                cbData.SetText(str);
            }catch(Exception ex)
            {
                Logger.Error($"{ModuleName}: Failed to read clipboard text: {ex.Message}");
            }
            
        }

        /// <summary>
        /// Gets a handle to a bitmap and converts to a byte array
        /// and stores it in the given clipboarddata object
        /// </summary>
        /// <param name="cbData"></param>
        private void ReadBitmap(ClipboardData cbData)
        {
            try
            {
                IntPtr gdiBitmap = GetClipboardData(WinClipboardDataFormat.CF_BITMAP);

                if (gdiBitmap == default)
                    throw new Win32Exception();

                using (Bitmap bmp = Bitmap.FromHbitmap(gdiBitmap))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        cbData.SetBitmap(ms.ToArray());
                    }
                }
            }catch(Exception ex)
            {
                Logger.Error($"{ModuleName}: Failed to read bitmap from clipboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads a list of files stored on the clipboard
        /// </summary>
        /// <param name="cbData"></param>
        private void ReadFileDrop(ClipboardData cbData)
        {
            try
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
                str = str[0..^2];
                //Split the string into seperate files
                var result = str.Split('\0');

                cbData.SetLocalFilePaths(result);
            }catch(Exception ex)
            {
                Logger.Error($"{ModuleName}: Failed to read HDROP from clipboard: {ex.Message}");
            }
            
        }

        protected override Task OnStop()
        {
            _window.Dispose();
            return Task.CompletedTask;
        }
    }
}
