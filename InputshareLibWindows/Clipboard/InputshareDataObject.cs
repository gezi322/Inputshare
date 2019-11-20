 using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using static InputshareLibWindows.Native.Ole32;
using static InputshareLibWindows.Native.Kernel32;
using static InputshareLibWindows.Native.User32;
using InputshareLibWindows.Native;
using System.Runtime.InteropServices;
using System.ComponentModel;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using System.Runtime.InteropServices.ComTypes;
using IStream = System.Runtime.InteropServices.ComTypes.IStream;
using System.Drawing;
using FileAttributes = InputshareLib.Clipboard.DataTypes.FileAttributes;
using System.Linq;

namespace InputshareLibWindows.Clipboard
{
    public class InputshareDataObject : IDataObject, IAsyncOperation
    {
        private static readonly uint InputshareClipboardFormatId;

        static InputshareDataObject()
        {
            InputshareClipboardFormatId = RegisterClipboardFormatA("InputshareData");
            ISLogger.Write("Registered clipboard format 'InputshareData'. Format = " + InputshareClipboardFormatId);
        }

        public event EventHandler ObjectPasted;

        public event EventHandler DropSuccess;

        private readonly ClipboardVirtualFileData clipboardFileData;
        private readonly MemoryStream fileDescriptorStream;
        private readonly List<ManagedRemoteIStream> streams = new List<ManagedRemoteIStream>();

        private short formatFileContentsId = (short)DataFormats.GetFormat("FileContents").Id;
        private short formatFileDescriptorId = (short)DataFormats.GetFormat("FileGroupDescriptorW").Id;
        private short formatTextId = (short)DataFormats.GetFormat(DataFormats.UnicodeText).Id;
        private short formatBitmapId = (short)DataFormats.GetFormat(DataFormats.Bitmap).Id;

        private IntPtr S_OK = new IntPtr(0);
        private IntPtr S_FALSE = new IntPtr(1);

        public Guid OperationGuid { get; private set; }

        private List<FORMATETC> supportedFormats = new List<FORMATETC>();

        private string storedText;
        private Bitmap storedImage;
        private bool isDragDropData;

        public InputshareDataObject(ClipboardDataBase data, bool isDragDrop)
        {
            isDragDropData = isDragDrop;
            OperationGuid = data.OperationId;

            supportedFormats.Add(new FORMATETC
            {
                cfFormat = (short)InputshareClipboardFormatId,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_HGLOBAL
            });

            if (data is ClipboardTextData cbText)
            {
                supportedFormats.Add(new FORMATETC
                {
                    cfFormat = formatTextId,
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex = -1,
                    ptd = IntPtr.Zero,
                    tymed = TYMED.TYMED_HGLOBAL
                });

                storedText = cbText.Text;
            }
            else if (data is ClipboardVirtualFileData cbFiles)
            {
                fileDescriptorStream = FILEDESCRIPTOR.GenerateFileDescriptor(cbFiles.AllFiles);
                clipboardFileData = cbFiles;

                supportedFormats.Add(new FORMATETC
                {
                    cfFormat = formatFileContentsId,
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex = -1,
                    ptd = IntPtr.Zero,
                    tymed = TYMED.TYMED_ISTREAM
                });

                supportedFormats.Add(new FORMATETC
                {
                    cfFormat = formatFileDescriptorId,
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex = -1,
                    ptd = IntPtr.Zero,
                    tymed = TYMED.TYMED_HGLOBAL
                });

                supportedFormats.Add(new FORMATETC
                {
                    cfFormat = (short)DataFormats.GetFormat("Preferred DropEffect").Id,
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex = -1,
                    ptd = IntPtr.Zero,
                    tymed = TYMED.TYMED_HGLOBAL
                });

            }
            else if (data is ClipboardImageData cbImage)
            {
                using (MemoryStream ms = new MemoryStream(cbImage.ImageData))
                {
                    storedImage = (Bitmap)Image.FromStream(ms);
                }

                supportedFormats.Add(new FORMATETC
                {
                    cfFormat = formatBitmapId,
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex = -1,
                    ptd = IntPtr.Zero,
                    tymed = TYMED.TYMED_GDI
                });

            }
        }

        ~InputshareDataObject()
        {
            if (storedImage != null)
                storedImage.Dispose();
        }

        public IEnumFORMATETC EnumFormatEtc(DATADIR direction)
        {
            int a = SHCreateStdEnumFmtEtc((uint)supportedFormats.Count, supportedFormats.ToArray(), out IEnumFORMATETC enumer);

            if (a != 0)
                throw new Win32Exception();

            return enumer;
        }


        public int GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut)
        {
            formatOut = new FORMATETC();
            return 1;
        }

        public void GetData(ref FORMATETC format, out STGMEDIUM medium)
        {
            medium = new STGMEDIUM();

            if (format.cfFormat == formatTextId)
                GetDataText(ref format, ref medium);
            else if (format.cfFormat == formatFileDescriptorId)
                GetDataFileDescriptor(ref format, ref medium);
            else if (format.cfFormat == formatFileContentsId)
                GetDataFileContents(ref format, ref medium);
            else if (format.cfFormat == formatBitmapId)
                GetDataBitmap(ref format, ref medium);
            else if (format.cfFormat == (short)InputshareClipboardFormatId)
                GetDataInputshareData(ref format, ref medium);
            //else
               //ISLogger.Write("Shell requested unsupported format {0} (using {1})", DataFormats.GetFormat(format.cfFormat).Name, format.tymed);

            return;
        }

        private void GetDataInputshareData(ref FORMATETC format, ref STGMEDIUM medium)
        {
            medium.tymed = TYMED.TYMED_HGLOBAL;

            byte[] rawGuid = OperationGuid.ToByteArray();

            IntPtr hMem = Marshal.AllocHGlobal(rawGuid.Length);
            Marshal.Copy(rawGuid, 0, hMem, rawGuid.Length);
            medium.unionmember = hMem;
        }

        private void GetDataFileContents(ref FORMATETC format, ref STGMEDIUM medium)
        {
            if (format.lindex == -1)
                return;

            if (streams.Count == 0)
            {
                try
                {
                    Guid token = clipboardFileData.RequestTokenMethod(clipboardFileData.OperationId).Result;
                    foreach(var file in clipboardFileData.AllFiles)
                    {
                        streams.Add(new ManagedRemoteIStream(file, clipboardFileData, token));
                    }
                }catch(Exception ex)
                {
                    ISLogger.Write("Failed to get access token for clipboard operation: " + ex.Message);
                    return;
                }
            }

            medium.tymed = TYMED.TYMED_ISTREAM;
            IStream o = streams[format.lindex];
            medium.unionmember = Marshal.GetComInterfaceForObject(o, typeof(IStream));
        }

        private void GetDataFileDescriptor(ref FORMATETC format, ref STGMEDIUM medium)
        {
            if (format.tymed != TYMED.TYMED_HGLOBAL)
            {
                ISLogger.Write("Shell requested file descriptor via tymed {0}. Not supported", format.tymed);
                return;
            }


            byte[] desc = fileDescriptorStream.ToArray();
            medium.tymed = TYMED.TYMED_HGLOBAL;
            medium.unionmember = CopyToHGlobal(desc);
        }

        private void GetDataBitmap(ref FORMATETC format, ref STGMEDIUM medium)
        {
            if (format.tymed != TYMED.TYMED_GDI)
            {
                ISLogger.Write("Shell requested bitmap via tymed {0}. Not supported", format.tymed);
                return;
            }

            medium.tymed = TYMED.TYMED_GDI;
            medium.unionmember = storedImage.GetHbitmap();
            medium.pUnkForRelease = null;
        }

        private void GetDataText(ref FORMATETC format, ref STGMEDIUM medium)
        {
            if (format.tymed != TYMED.TYMED_HGLOBAL)
            {
                ISLogger.Write("Shell requested text via tymed {0}. Not supported", format.tymed);
                return;
            }

            try
            {
                medium.tymed = TYMED.TYMED_HGLOBAL;
                byte[] textData = Encoding.Unicode.GetBytes(storedText);
                medium.unionmember = CopyToHGlobal(textData);
                medium.pUnkForRelease = null;
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to return text data to shell ({0}): {1}", format.tymed, ex.Message);
            }
        }

        private IntPtr CopyToHGlobal(byte[] data)
        {
            IntPtr ptr = Marshal.AllocHGlobal(data.Length);

            if (ptr == IntPtr.Zero)
                throw new Win32Exception();

            Marshal.Copy(data, 0, ptr, data.Length);
            return ptr;
        }

        public void GetDataHere(ref FORMATETC format, ref STGMEDIUM medium)
        {
            if (format.cfFormat == formatFileDescriptorId)
                GetDataFileDescriptor(ref format, ref medium);
            else if (format.cfFormat == formatFileContentsId)
                GetDataFileContents(ref format, ref medium);
        }

        public int QueryGetData(ref FORMATETC format)
        {
            foreach (var f in supportedFormats)
                if (f.cfFormat == format.cfFormat && f.tymed == format.tymed)
                    return (int)S_OK;

            return (int)S_FALSE;
        }

        public void SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release)
        {
        }

        public int DAdvise([In] ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection)
        {
            connection = -1;
            return (int)S_FALSE;
        }

        public void DUnadvise(int connection)
        {
        }

        public int EnumDAdvise(out IEnumSTATDATA enumAdvise)
        {
            enumAdvise = null;
            return (int)S_FALSE;
        }


        private const int VARIANT_TRUE = -1;
        private const int VARIANT_FALSE = 0;

        private bool asyncInOperation = false;
        public void SetAsyncMode([In] int fDoOpAsync)
        {

        }

        public void GetAsyncMode([Out] out int pfIsOpAsync)
        {
            pfIsOpAsync = VARIANT_TRUE;
        }

        public void StartOperation([In] IBindCtx pbcReserved)
        {
            asyncInOperation = true;

            if (isDragDropData)
                DropSuccess?.Invoke(this, new EventArgs());
            else
                ObjectPasted?.Invoke(this, new EventArgs());
        }

        public void InOperation([Out] out int pfInAsyncOp)
        {
            pfInAsyncOp = asyncInOperation ? VARIANT_TRUE : VARIANT_FALSE;
        }

        public void EndOperation([In] int hResult, [In] IBindCtx pbcReserved, [In] uint dwEffects)
        {
            asyncInOperation = false;

            //This method is called when the file data (could be pasted clipboard data, or a dragdrop operation)
            //has finished transfering to the target application. We can use this if we need a DataTransferComplete event.
            //This method also gets called if the operation fails or is cancelled
        }
    }

}
