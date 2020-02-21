using Inputshare.Common.Clipboard;
using Inputshare.Common.Net.RFS;
using Inputshare.Common.Net.RFS.Client;
using Inputshare.Common.PlatformModules.Windows.Native.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using static Inputshare.Common.PlatformModules.Windows.Native.Ole32;
using static Inputshare.Common.PlatformModules.Windows.Native.User32;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;

namespace Inputshare.Common.PlatformModules.Windows.Clipboard
{
    /// <summary>
    /// An object that can be placed on the windows clipboard
    /// </summary>
    internal class ClipboardDataObject : Native.Interfaces.IDataObject, IAsyncOperation
    {
        internal event EventHandler<ClipboardDataObject> FilesPasted;

        private FORMATETC[] _formats;
        internal ClipboardData InnerData { get; private set; }

        private NativeRFSStream[] _fileStreams;
        private Guid _fileStreamToken;

#pragma warning disable IDE0060

        internal static  ClipboardDataObject Create(ClipboardData cbData)
        {
            ClipboardDataObject cbobj = new ClipboardDataObject
            {
                InnerData = cbData
            };

            cbobj.CreateCompatibleFormats(cbData);
            return cbobj;
        }

        public IntPtr GetData([In] ref FORMATETC format, out STGMEDIUM medium)
        {
            medium = new STGMEDIUM
            {
                tymed = TYMED.TYMED_NULL
            };

            if (format.cfFormat == WinClipboardDataFormat.CF_UNICODETEXT)
                GetTextHGlobal(ref format, ref medium);
            else if (format.cfFormat == WinClipboardDataFormat.CFSTR_FILEDESCRIPTOR)
                GetFileDescriptor(ref format, ref medium);
            else if (format.cfFormat == WinClipboardDataFormat.CFSTR_FILECONTENTS)
                GetFileContentsStream(ref format, ref medium);
            else if (format.cfFormat == WinClipboardDataFormat.PREFERREDDROPEFFECT)
                GetDropEffect(ref format, ref medium);
            else if (format.cfFormat == WinClipboardDataFormat.CF_BITMAP)
                GetDataBitmap(ref format, ref medium);

            return IntPtr.Zero;
        }

        public IntPtr GetDataHere([In] ref FORMATETC format, ref STGMEDIUM medium)
        {
            if (format.cfFormat == WinClipboardDataFormat.CFSTR_FILEDESCRIPTOR)
                GetFileDescriptor(ref format, ref medium);
            else if (format.cfFormat == WinClipboardDataFormat.CFSTR_FILECONTENTS)
                GetFileContentsStream(ref format, ref medium);
                

            return IntPtr.Zero;
        }

        public IntPtr QueryGetData([In] ref FORMATETC format)
        {
            if((int)format.tymed == -1)
            {
                foreach (var supportedFormat in _formats)
                    if (supportedFormat.cfFormat == format.cfFormat)
                        return new IntPtr(0);
            }
            else
            {
                foreach (var supportedFormat in _formats)
                    if (supportedFormat.cfFormat == format.cfFormat && supportedFormat.tymed == format.tymed)
                        return new IntPtr(0);
            }

            return new IntPtr(1);
        }

        public IntPtr GetCanonicalFormatEtc([In] ref FORMATETC formatIn, out FORMATETC formatOut)
        {
            formatOut = new FORMATETC();
            return new IntPtr(0x80004001);
        }

        public IntPtr SetData([In] ref FORMATETC formatIn, [In] ref STGMEDIUM medium, [MarshalAs(UnmanagedType.Bool)] bool release)
        {
            return new IntPtr(0x80004001);
        }

        public IntPtr EnumFormatEtc(DATADIR direction, out IEnumFORMATETC ppenumFormatEtc)
        {
            IntPtr x = SHCreateStdEnumFmtEtc((uint)_formats.Length, _formats, out ppenumFormatEtc);
            return x;
        }

        public IntPtr DAdvise([In] ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection)
        {
            connection = -1;
            return new IntPtr(0x80004001);
        }

        public IntPtr DUnadvise(int connection)
        {
            return new IntPtr(0x80004001);
        }

        public IntPtr EnumDAdvise(out IEnumSTATDATA enumAdvise)
        {
            enumAdvise = null;
            return new IntPtr(0x80004001);
        }

        /// <summary>
        /// Creates a formatetc array of compatible data formats stored in the given
        /// ClipboardData object
        /// </summary>
        /// <param name="cbData"></param>
        /// <returns></returns>
        private void CreateCompatibleFormats(ClipboardData cbData)
        {
            List<FORMATETC> formats = new List<FORMATETC>();

            formats.Add(CreateTempFormat());

            if (cbData.IsTypeAvailable(ClipboardDataType.UnicodeText))
                formats.Add(CreateUnicodeFormat());

            if (cbData.IsTypeAvailable(ClipboardDataType.RemoteFileGroup))
            {
                try
                {
                    _fileStreams = new NativeRFSStream[cbData.GetRemoteFiles().Files.Length];
                    formats.Add(CreateFileContentsFormat());
                    formats.Add(CreateFileDescriptorWFormat());
                    formats.Add(CreatePreferredEffectFormat());
                }catch(Exception ex)
                {
                    Logger.Write(ex.Message);
                    Logger.Write(ex.StackTrace);
                } 
            }

            if (cbData.IsTypeAvailable(ClipboardDataType.Bitmap))
                formats.Add(CreateBitmapFormat());
                

            _formats = formats.ToArray();
        }
        private FORMATETC CreateTempFormat()
        {
            return new FORMATETC
            {
                cfFormat = (short)WinClipboardDataFormat.InputshareFormat,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_NULL
            };
        }
        
        private FORMATETC CreateUnicodeFormat()
        {
            return new FORMATETC
            {
                cfFormat = (short)WinClipboardDataFormat.CF_UNICODETEXT,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_HGLOBAL
            };
        }

        private FORMATETC CreateFileContentsFormat()
        {
            return new FORMATETC
            {
                cfFormat = (short)WinClipboardDataFormat.CFSTR_FILECONTENTS,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_ISTREAM
            };
        }

        private FORMATETC CreateFileDescriptorWFormat()
        {
            return new FORMATETC
            {
                cfFormat = (short)WinClipboardDataFormat.CFSTR_FILEDESCRIPTOR,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_HGLOBAL
            };
        }

        private FORMATETC CreatePreferredEffectFormat()
        {
            return new FORMATETC
            {
                cfFormat = WinClipboardDataFormat.PREFERREDDROPEFFECT,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_HGLOBAL
            };
        }

        private FORMATETC CreateBitmapFormat()
        {
            return new FORMATETC
            {
                cfFormat = (short)WinClipboardDataFormat.CF_BITMAP,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_GDI
            };
        }

        private void GetTextHGlobal(ref FORMATETC format, ref STGMEDIUM medium)
        {
            try
            {
                medium.tymed = TYMED.TYMED_HGLOBAL;
                byte[] text = Encoding.Unicode.GetBytes(InnerData.GetText());
                medium.unionmember = CopyHGlobal(text);
                medium.pUnkForRelease = IntPtr.Zero;
            }catch(Exception ex)
            {
                Logger.Write($"Failed to return text to shell: {ex.Message}");
            }
        }

        private IntPtr CopyHGlobal(byte[] data)
        {
            IntPtr ptr = Marshal.AllocHGlobal(data.Length);
            if (ptr == IntPtr.Zero)
                throw new Win32Exception();

            Marshal.Copy(data, 0, ptr, data.Length);
            return ptr;
        }

        private void GetFileDescriptor(ref FORMATETC format, ref STGMEDIUM medium)
        {

            try
            {
                RFSFileGroup group = InnerData.GetRemoteFiles();
                var memStr = FILEDESCRIPTOR.GenerateFileDescriptor(group);
                IntPtr ptr = CopyHGlobal(memStr.ToArray());
                memStr.Dispose();


                medium.tymed = TYMED.TYMED_HGLOBAL;
                medium.unionmember = ptr;
                medium.pUnkForRelease = null;
            }
            catch(Exception ex)
            {
                Logger.Write("Failed to return file descriptor to shell: " + ex.Message);
            }
        }

        private void GetDropEffect(ref FORMATETC format, ref STGMEDIUM medium)
        {
            byte[] b = BitConverter.GetBytes(1);
            medium.pUnkForRelease = null;
            medium.tymed = TYMED.TYMED_HGLOBAL;
            medium.unionmember = CopyHGlobal(b);
        }

        private void GetFileContentsStream(ref FORMATETC format, ref STGMEDIUM medium)
        {
            if (format.lindex == -1)
                return;

            try
            {
                
                int index = format.lindex;

                //Get a token to read the file group
                if (_fileStreamToken == Guid.Empty)
                    _fileStreamToken = (InnerData.GetRemoteFiles() as RFSClientFileGroup).GetToken();

                if (_fileStreams[index] == null)
                {
                    var group = (InnerData.GetRemoteFiles() as RFSClientFileGroup);
                    _fileStreams[index] = new NativeRFSStream(group.CreateStream(group.Files[index], _fileStreamToken));
                    
                }
                medium.tymed = TYMED.TYMED_ISTREAM;
                IStream str = _fileStreams[index];
                medium.unionmember = Marshal.GetComInterfaceForObject(str, typeof(IStream));
            }catch(Exception ex)
            {
                Logger.Write("Failed to return filestream to shell: " + ex.Message);
            }
        }

        private void GetDataBitmap(ref FORMATETC format, ref STGMEDIUM medium)
        {
            using (MemoryStream ms = new MemoryStream(InnerData.GetBitmapSerialized()))
            {
                using (Bitmap bmp = (Bitmap)Bitmap.FromStream(ms))
                {
                    medium.tymed = TYMED.TYMED_GDI;
                    medium.unionmember = bmp.GetHbitmap();
                    medium.pUnkForRelease = null;
                }
            }
               
        }

        private bool _isInAsyncOperation = false;

        public void SetAsyncMode([In] int fDoOpAsync)
        {

        }

        public void GetAsyncMode([Out] out int pfIsOpAsync)
        {
            pfIsOpAsync = -1;
        }

        public void StartOperation([In] IBindCtx pbcReserved)
        {
            _isInAsyncOperation = true;
            FilesPasted?.Invoke(this, this);
        }

        public void InOperation([Out] out int pfInAsyncOp)
        {
            pfInAsyncOp = _isInAsyncOperation ? 0 : -1;
        }

        public void EndOperation([In] int hResult, [In] IBindCtx pbcReserved, [In] uint dwEffects)
        {
            _isInAsyncOperation = false;
        }

#pragma warning restore IDE0060
    }
}
