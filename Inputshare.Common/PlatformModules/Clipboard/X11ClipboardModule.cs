using Inputshare.Common.Clipboard;
using Inputshare.Common.PlatformModules.Linux;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11Structs;
using static Inputshare.Common.PlatformModules.Linux.Native.LibXfixes;

namespace Inputshare.Common.PlatformModules.Clipboard
{
    public class X11ClipboardModule : ClipboardModuleBase
    {
        public override event EventHandler<ClipboardData> ClipboardChanged;

        private XConnection _connection;
        private IntPtr _xDisplay;
        private IntPtr _xRootWindow;
        private IntPtr _xWindow;
        private MemoryStream _incrBuffer;
        private ClipboardData _innerData;

        #region atoms
        private IntPtr _atomClipboard => XInternAtom(_xDisplay, "CLIPBOARD", false);
        private IntPtr _atomPrimary => XInternAtom(_xDisplay, "PRIMARY", false);
        private IntPtr _atomTargets => XInternAtom(_xDisplay, "TARGETS", false);
        private IntPtr _atomUtf8String => XInternAtom(_xDisplay, "UTF8_STRING", false);
        private IntPtr _atomText => XInternAtom(_xDisplay, "TEXT", false);
        private IntPtr _atomString => XInternAtom(_xDisplay, "STRING", false);
        private IntPtr _atomImagePng => XInternAtom(_xDisplay, "image/png", false);
        private IntPtr _atomUriListReturn => XInternAtom(_xDisplay, "cbReturnPropFile", false);
        private IntPtr _atomTextReturn => XInternAtom(_xDisplay, "cbReturnPropText", false);
        private IntPtr _atomImageReturn => XInternAtom(_xDisplay, "cbReturnPropImage", false);
        private IntPtr _atomINCR => XInternAtom(_xDisplay, "INCR", false);
        private IntPtr _atomUriList => XInternAtom(_xDisplay, "text/uri-list", false);
        #endregion

        public X11ClipboardModule(XConnection connection)
        {
            _connection = connection;
            _xDisplay = connection.XDisplay;
            _xRootWindow = XDefaultRootWindow(_xDisplay);
        }

        public override Task SetClipboardAsync(ClipboardData cbData)
        {
            _innerData = cbData;

            XSetSelectionOwner(_xDisplay, _atomClipboard, _xWindow);

            if (XGetSelectionOwner(_xDisplay, _atomClipboard) != _xWindow)
                Logger.Error($"{ModuleName}: Failed to set clipboard owner");
            else
                Logger.Debug($"{ModuleName}: Set clipboard owner");

            return Task.CompletedTask;
        }

        private void HandleSelectionRequest(XSelectionRequestEvent evt)
        {
            XEvent retEvent = new XEvent();
            retEvent.type = XEventName.SelectionNotify;
            retEvent.SelectionEvent.display = evt.display;
            retEvent.SelectionEvent.requestor = evt.requestor;
            retEvent.SelectionEvent.selection = evt.selection;
            retEvent.SelectionEvent.time = evt.time;
            retEvent.SelectionEvent.target = evt.target;
            retEvent.SelectionEvent.property = evt.property;

            Logger.Debug($"{ModuleName}: Handling selection request for type {GetAtomName(evt.target)}");

            if (evt.target == _atomTargets)
                HandleTargetsRequest(evt);
            else if (evt.target == _atomString || evt.target == _atomText || evt.target == _atomUtf8String)
                HandleTextRequest(evt);
            else if (evt.target == _atomImagePng)
                HandleRequestImage(evt);

            XSendEvent(_xDisplay, retEvent.SelectionEvent.requestor, true, 0, ref retEvent);
        }

        private void HandleTextRequest(XSelectionRequestEvent evt)
        {
            Logger.Debug($"{ModuleName}: Handling request for text");

            string text = _innerData.GetText();
            XChangeProperty(_xDisplay, evt.requestor, evt.property, _atomUtf8String, 8, 0, Encoding.UTF8.GetBytes(text), text.Length);
        }

        private void HandleRequestImage(XSelectionRequestEvent evt)
        {
            Logger.Debug($"{ModuleName}: Handling request for image");
            var imageData = _innerData.GetBitmapSerialized();
            XChangeProperty(_xDisplay, evt.requestor, evt.property, _atomImagePng, 8, 0, imageData, imageData.Length);
        }

        private void HandleTargetsRequest(XSelectionRequestEvent evt)
        {
            if(_innerData == null)
            {
                Logger.Warning($"{ModuleName}: Ignoring targets request as no data is available");
                return;
            }

            byte[] rawData = new byte[0];
            if (_innerData.IsTypeAvailable(ClipboardDataType.UnicodeText))
                rawData = GetAtomsBytesText();
            else if (_innerData.IsTypeAvailable(ClipboardDataType.Bitmap))
                rawData = GetAtomsBytesImage();
            else
                Logger.Warning($"{ModuleName}: Returning 0 targets");

            XChangeProperty(_xDisplay, evt.requestor, evt.property, new IntPtr(4), 32, 0, rawData, 1);
        }

        private byte[] GetAtomsBytesText()
        {
            long[] allowedAtoms = new long[]
            {
                (long)_atomString,
                (long)_atomText,
                (long)_atomUtf8String,
            };

            return LongArrayToByteArray(allowedAtoms);
        }

        private byte[] GetAtomsBytesImage()
        {
            long[] allowedAtoms = new long[]
            {
                (long)_atomImagePng
            };

            return LongArrayToByteArray(allowedAtoms);
        }

        private byte[] LongArrayToByteArray(long[] array)
        {
            List<byte> bytes = new List<byte>();
            foreach (var item in array)
            {
                foreach (var b in BitConverter.GetBytes(item))
                    bytes.Add(b);
            }

            return bytes.ToArray();
        }

        protected override Task OnStart()
        {
            _connection.EventReceived += OnEventReceived;
            CreateWindow();
            return Task.CompletedTask;
        }
        protected override Task OnStop()
        {
            return Task.CompletedTask;
        }

        private void OnEventReceived(object sender, XEvent evt)
        {
            if (evt.AnyEvent.window != _xWindow && evt.AnyEvent.window != _xRootWindow)
                return;

            Logger.Verbose($"{ModuleName}: Handling type {evt.type}");

            try
            {
                if (evt.type == XEventName.PropertyNotify)
                    HandlePropertyChange(evt.PropertyEvent);
                else if (evt.type == XEventName.XFixesNotify)
                    HandleXFixesNotify(evt.XFixesNotifyEvent);
                else if (evt.type == XEventName.SelectionNotify)
                    HandleSelectionNotify(evt.SelectionEvent);
                else if (evt.type == XEventName.SelectionRequest)
                    HandleSelectionRequest(evt.SelectionRequestEvent);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ModuleName}: An error occurred handling event type {evt.type}: {ex.Message}");
                Logger.Error(ex.StackTrace);
            }
        }

        private void HandleSelectionNotify(XSelectionEvent evt)
        {
            if (evt.property == _atomTargets)
                HandleReceivedTargets(evt);
            else if (evt.property == _atomTextReturn)
                HandleReceivedText(_atomTextReturn);
            else if (evt.property == _atomImageReturn)
                HandleReceivedImage(evt);
            else if (evt.property == _atomUriListReturn)
                HandleReceivedFileDrop(evt);
        }

        private void HandleReceivedTargets(XSelectionEvent evt)
        {
            Logger.Verbose($"{ModuleName}: Received targets from selection owner");

            //determine the size of the received data
            XGetWindowProperty(_xDisplay, evt.requestor, _atomTargets, 0, 0, false, IntPtr.Zero, out _, out _, out _,
                out int dataSize, out _);

            XGetWindowProperty(_xDisplay, evt.requestor, _atomTargets, 0, dataSize, false, new IntPtr(0), out IntPtr retType, out int format,
              out int numItems, out int remBytes, out IntPtr buff);

            try
            {
                format = format == 32 ? 64 : format;

                IntPtr[] targets = new IntPtr[numItems];
                for (int i = 0; i < numItems; i++)
                {
                    int offset = i * (format / 8);
                    targets[i] = (IntPtr)Marshal.ReadInt64(buff, offset);
                    Logger.Debug($"{ModuleName}: Received target {GetAtomName(targets[i])}");
                }

                XFree(buff);
                ProcessTargetList(targets, evt.requestor);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ModuleName}: Failed to read received targets list: {ex.Message}");
            }
        }

        private void ProcessTargetList(IntPtr[] targets, IntPtr ownerWindow)
        {
            Logger.Information($"{ModuleName}: Got {targets.Length} targets");

            foreach (var target in targets)
            {
                if (target == _atomText)
                    RequestSelection(target, ownerWindow, _atomTextReturn);
                else if (target == _atomImagePng)
                    RequestSelection(target, ownerWindow, _atomImageReturn);
                else if (target == _atomUriList)
                    RequestSelection(target, ownerWindow, _atomUriListReturn);
            }
        }

        private void HandleReceivedImage(XSelectionEvent evt)
        {
            int ret = XGetWindowProperty(_xDisplay, _xWindow, evt.property, 0, 0, false, new IntPtr(0), out IntPtr retType,
              out int format, out int nItems, out int dataSize, out IntPtr prop_return);
            XFree(prop_return);

            XGetWindowProperty(_xDisplay, _xWindow, evt.property, 0, dataSize, false, new IntPtr(0), out IntPtr returned_type,
                out format, out nItems, out int remBytes, out prop_return);

            try
            {
                if (returned_type == XInternAtom(_xDisplay, "INCR", false))
                {
                    OnINCRTransferStart(evt, returned_type, prop_return);
                    return;
                }

                byte[] imgData = new byte[nItems];
                Marshal.Copy(prop_return, imgData, 0, nItems);
                XFree(prop_return);

                ClipboardData cbData = new ClipboardData();
                cbData.SetBitmap(imgData);
                ClipboardChanged?.Invoke(this, cbData);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ModuleName}: Failed to read received image: {ex.Message}");
            }
        }

        private void OnINCRTransferStart(XSelectionEvent evt, IntPtr returned_type, IntPtr prop_return)
        {
            int dataSize = Marshal.ReadInt32(prop_return);
            Logger.Debug($"{ModuleName}: Starting INCR transfer ({dataSize} bytes)");
            _incrBuffer = new MemoryStream(dataSize);
            XDeleteProperty(_xDisplay, _xWindow, evt.property);
        }

        private void RequestSelection(IntPtr dataType, IntPtr owner, IntPtr returnAtom)
        {
            XEvent evt = new XEvent();
            evt.type = XEventName.SelectionRequest;
            evt.SelectionRequestEvent.display = _xDisplay;
            evt.SelectionRequestEvent.owner = owner;
            evt.SelectionRequestEvent.requestor = _xWindow;
            evt.SelectionRequestEvent.selection = _atomClipboard;
            evt.SelectionRequestEvent.target = dataType;
            XConvertSelection(_xDisplay, _atomClipboard, dataType, returnAtom, _xWindow, IntPtr.Zero);
        }

        private void HandleReceivedText(IntPtr property)
        {
            int ret = XGetWindowProperty(_xDisplay, _xWindow, property, 0, 0, false, new IntPtr(0), out IntPtr retType,
                out int format, out int nItems, out int dataSize, out IntPtr prop_return);
            XFree(prop_return);

            XGetWindowProperty(_xDisplay, _xWindow, property, 0, dataSize, false, new IntPtr(0), out IntPtr returned_type,
                out format, out nItems, out int remBytes, out prop_return);

            if (remBytes > 0)
                Logger.Warning($"{ModuleName}: HandleReceivedText data only partially read");

            if (prop_return == IntPtr.Zero)
            {
                Logger.Error($"{ModuleName}: Failed to read from text atom");
                return;
            }

            try
            {
                string text = Marshal.PtrToStringUTF8(prop_return, nItems);
                XFree(prop_return);

                ClipboardData cbData = new ClipboardData();
                cbData.SetText(text);
                ClipboardChanged?.Invoke(this, cbData);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ModuleName}: Failed to read returned text: {ex.Message}");
            }
        }

        private void HandleReceivedFileDrop(XSelectionEvent evt)
        {
            try
            {
                int ret = XGetWindowProperty(_xDisplay, _xWindow, evt.property, 0, 0, false, new IntPtr(0), out IntPtr retType,
                   out int format, out int nItems, out int dataSize, out IntPtr prop_return);
                XFree(prop_return);

                XGetWindowProperty(_xDisplay, _xWindow, evt.property, 0, dataSize, false, new IntPtr(0), out IntPtr returned_type,
                    out format, out nItems, out int remBytes, out prop_return);

                string fList = Marshal.PtrToStringAuto(prop_return, nItems);
                XFree(prop_return);

                string[] rawList = fList.Split('\n');
                string[] fixedList = new string[rawList.Length];

                for (int i = 0; i < fixedList.Length; i++)
                    fixedList[i] = rawList[i].Trim().Replace("file://", "");

                ClipboardData cbData = new ClipboardData();
                cbData.SetLocalFilePaths(fixedList);
                ClipboardChanged?.Invoke(this, cbData);
            }
            catch(Exception ex)
            {
                Logger.Error($"{ModuleName}: Failed to read received file drop list: {ex.Message}");
                Logger.Error(ex.StackTrace);
            }
        }

        private void HandleXFixesNotify(XFixesSelectionNotifyEvent evt)
        {
            if (evt.selection == _atomPrimary)
            {
                Logger.Warning($"{ModuleName}: Primary selection changed (not yet implemented)");
                return;
            }

            if(XGetSelectionOwner(_xDisplay, _atomClipboard) == _xWindow)
            {
                Logger.Debug($"{ModuleName}: Ignoring clipboard change set by current window");
                return;
            }

            Logger.Debug($"{ModuleName}: Clipboard selection changed");
            IntPtr selectionOwner = XGetSelectionOwner(_xDisplay, _atomClipboard);
            RequestTargetsFromWindow(selectionOwner, _atomClipboard);
        }

        private string GetAtomName(IntPtr atom)
        {
            return XGetAtomName(_xDisplay, atom);
        }

        private void RequestTargetsFromWindow(IntPtr window, IntPtr selection)
        {
            XEvent evt = new XEvent();
            evt.type = XEventName.SelectionRequest;
            evt.SelectionRequestEvent.display = _xDisplay;
            evt.SelectionRequestEvent.owner = window;
            evt.SelectionRequestEvent.selection = selection;
            evt.SelectionRequestEvent.requestor = _xWindow;
            evt.SelectionRequestEvent.property = _atomTargets;
            evt.SelectionRequestEvent.target = _atomTargets;
            XSendEvent(_xDisplay, window, true, 0, ref evt);
            Logger.Verbose($"{ModuleName}: Requesting selection target from owner");
        }

        private void HandlePropertyChange(XPropertyEvent evt)
        {
            //A state of 1 means a property was deleted
            if (_incrBuffer == null  || evt.atom != _atomImageReturn || evt.state != 0)
            {
                Logger.Debug($"{ModuleName}: Ignoring property change for atom {GetAtomName(evt.atom)}");
                return;
            }

            int ret = XGetWindowProperty(_xDisplay, _xWindow, evt.atom, 0, 0, true, new IntPtr(0), out IntPtr retType,
               out int format, out int nItems, out int dataSize, out IntPtr prop_return);
            XFree(prop_return);

            int rem = (int)(_incrBuffer.Capacity - _incrBuffer.Position);
            if (rem < dataSize)
                dataSize = rem;

            XGetWindowProperty(_xDisplay, _xWindow, evt.atom, 0, dataSize, false, new IntPtr(0), out IntPtr returned_type,
                    out format, out nItems, out int remBytes, out prop_return);

            if ((dataSize == 0 && _incrBuffer.Position == 0))
            {
                Logger.Error($"{ModuleName}: INCR transfer failed at {_incrBuffer.Position}/{_incrBuffer.Capacity}");
                _incrBuffer?.Dispose();
                _incrBuffer = null;
                XFree(prop_return);
                return;
            }

            byte[] buff = new byte[dataSize];
            Marshal.Copy(prop_return, buff, 0, dataSize);
            _incrBuffer.Write(buff);
            Logger.Fatal($"{ModuleName}: INCR transfer {_incrBuffer.Position}/{_incrBuffer.Capacity}");
            XDeleteProperty(_xDisplay, _xWindow, evt.atom);
            XFree(prop_return);
            XFlush(_xDisplay);

            if (_incrBuffer.Position == _incrBuffer.Capacity)
            {
                ClipboardData cbData = new ClipboardData();
                cbData.SetBitmap(_incrBuffer.ToArray());
                _incrBuffer.Dispose();
                _incrBuffer = null;
                ClipboardChanged?.Invoke(this, cbData);
            }
        }

        private void CreateWindow()
        {
            _xWindow = XCreateSimpleWindow(_xDisplay, _xRootWindow, 0, 0, 1, 1, 0, UIntPtr.Zero, UIntPtr.Zero);
            XFlush(_xDisplay);
            XSelectInput(_xDisplay, _xWindow, EventMask.PropertyChangeMask);
            XFixesSelectSelectionInput(_xDisplay, _xWindow, _atomClipboard, 1);
        }
    }
}
