using Inputshare.Common.Clipboard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11Structs;

namespace Inputshare.Common.PlatformModules.Linux.Clipboard
{
    /// <summary>
    /// Reads selection data received from other windows
    /// </summary>
    internal class X11ClipboardReader
    {
        internal event EventHandler<byte[]> INCRImageReceived;

        private readonly IntPtr _xDisplay;
        private readonly IntPtr _xWindow;
        private readonly X11AtomList _atoms;
        private readonly MemoryStream _incrBuffer;

        internal X11ClipboardReader(IntPtr xDisplay, IntPtr window, X11AtomList atoms)
        {
            _xDisplay = xDisplay;
            _xWindow = window;
            _atoms = atoms;
            _incrBuffer = new MemoryStream();
        }

        /// <summary>
        /// Sends a request to the specified window for a list of targets 
        /// for the specified selection (primary or clipboard)
        /// </summary>
        /// <param name="window"></param>
        /// <param name="selection"></param>
        internal void SentTargetsRequest(IntPtr window, IntPtr selection)
        {
            XEvent evt = new XEvent();
            evt.type = XEventName.SelectionRequest;
            evt.SelectionRequestEvent.display = _xDisplay;
            evt.SelectionRequestEvent.owner = window;
            evt.SelectionRequestEvent.selection = selection;
            evt.SelectionRequestEvent.requestor = _xWindow;
            evt.SelectionRequestEvent.property = _atoms.Targets;
            evt.SelectionRequestEvent.target = _atoms.Targets;
            XSendEvent(_xDisplay, window, true, 0, ref evt);
            Logger.Verbose($"Requesting selection target from owner");
        }

        /// <summary>
        /// Reads a text property set by another window
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        internal string ReadText(XSelectionEvent evt)
        {
            XGetWindowProperty(_xDisplay, _xWindow, evt.property, 0, 0, false, new IntPtr(0), out _,
                out _, out _, out int dataSize, out IntPtr prop_return);
            XFree(prop_return);

            XGetWindowProperty(_xDisplay, _xWindow, evt.property, 0, dataSize, false, new IntPtr(0), out _,
                out _, out int nItems, out _, out prop_return);

            string text = Marshal.PtrToStringUTF8(prop_return, nItems);
            XFree(prop_return);
            return text;
        }

        /// <summary>
        /// Reads a list of files set by another window
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        internal string[] ReadFileDropList(XSelectionEvent evt)
        {
            XGetWindowProperty(_xDisplay, _xWindow, evt.property, 0, 0, false, new IntPtr(0), out _,
                   out _, out _, out int dataSize, out IntPtr prop_return);
            XFree(prop_return);

            XGetWindowProperty(_xDisplay, _xWindow, evt.property, 0, dataSize, false, new IntPtr(0), out _,
                out _, out int nItems, out _, out prop_return);

            string fList = Marshal.PtrToStringAuto(prop_return, nItems);
            XFree(prop_return);

            string[] rawList = fList.Split('\n');
            string[] fixedList = new string[rawList.Length];

            for (int i = 0; i < fixedList.Length; i++)
                fixedList[i] = rawList[i].Trim().Replace("file://", "");

            return fixedList;
        }

        /// <summary>
        /// Reads a list of compatible clipboard targets from the window that
        /// sent the selectionnotify
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        internal IntPtr[] ReadTargetsList(XSelectionEvent evt)
        {
            XGetWindowProperty(_xDisplay, evt.requestor, _atoms.Targets, 0, 0, false, IntPtr.Zero, out _, out _, out _,
                out int dataSize, out _);

            XGetWindowProperty(_xDisplay, evt.requestor, _atoms.Targets, 0, dataSize, false, new IntPtr(0), out _, out int format,
              out int numItems, out _, out IntPtr buff);

            format = format == 32 ? 64 : format;

            IntPtr[] targets = new IntPtr[numItems];
            for (int i = 0; i < numItems; i++)
            {
                int offset = i * (format / 8);
                targets[i] = (IntPtr)Marshal.ReadInt64(buff, offset);
            }

            XFree(buff);

            return targets;
        }

        /// <summary>
        /// Reads a serialized image set by another window.
        /// 
        /// Returns 0 bytes if the image is going to be received via INCR
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        internal byte[] ReadImagePng(XSelectionEvent evt)
        {
            XGetWindowProperty(_xDisplay, _xWindow, evt.property, 0, 0, false, new IntPtr(0), out _,
              out _, out _, out int dataSize, out IntPtr prop_return);
            XFree(prop_return);

            XGetWindowProperty(_xDisplay, _xWindow, evt.property, 0, dataSize, false, new IntPtr(0), out IntPtr returned_type,
                out _, out int nItems, out _, out prop_return);

            //Larger images are usually transfered via INCR transfers
            if(returned_type == XInternAtom(_xDisplay, "INCR", false))
            {
                OnINCRTransferStart(evt, Marshal.ReadInt32(prop_return));
                return new byte[0];
            }

            byte[] imgData = new byte[nItems];
            Marshal.Copy(prop_return, imgData, 0, nItems);
            XFree(prop_return);
            return imgData;
        }

        private void OnINCRTransferStart(XSelectionEvent evt, int dataSize)
        {
            Logger.Debug($"Starting INCR transfer " + dataSize/1024 + "KB");
            _incrBuffer.SetLength(dataSize);

            //The window that sends the image waits for the given atom (evt.property)
            //to be deleted from this window. When the property is deleted from this window,
            //the sender window will begin sending the image in small increments
            XDeleteProperty(_xDisplay, _xWindow, evt.property);
        }

        /// <summary>
        /// Handles an INCR property set by another window
        /// </summary>
        /// <param name="evt"></param>
        internal void HandleINCRImagePropertySet(XPropertyEvent evt)
        {
            if (_incrBuffer.Capacity == 0 || evt.atom != _atoms.ImageReturn || evt.state != 0)
                return;

            int ret = XGetWindowProperty(_xDisplay, _xWindow, evt.atom, 0, 0, true, new IntPtr(0), out IntPtr retType,
               out _, out _, out int dataSize, out IntPtr prop_return);
            XFree(prop_return);

            int rem = (int)(_incrBuffer.Capacity - _incrBuffer.Position);
            if (rem < dataSize)
                dataSize = rem;

            XGetWindowProperty(_xDisplay, _xWindow, evt.atom, 0, dataSize, false, new IntPtr(0), out IntPtr returned_type,
                    out int format, out int nItems, out int remBytes, out prop_return);

            if(_incrBuffer.Position == 0 && dataSize == 0)
            {
                Logger.Error($"INCR image transfer failed at {_incrBuffer.Position/1024}KB/{_incrBuffer.Capacity/1024}KB");
                _incrBuffer.SetLength(0);
                XFree(prop_return);
                return;
            }

            byte[] buff = new byte[dataSize];
            Marshal.Copy(prop_return, buff, 0, dataSize);
            _incrBuffer.Write(buff);
            Logger.Verbose($"INCR transfer: {_incrBuffer.Position/1024}KB/{_incrBuffer.Capacity/1024}KB");
            XDeleteProperty(_xDisplay, _xWindow, evt.atom);
            XFree(prop_return);
            XFlush(_xDisplay);

            if (_incrBuffer.Position == _incrBuffer.Capacity)
            {
                Logger.Debug("INCR transfer complete");
                INCRImageReceived?.Invoke(this, _incrBuffer.ToArray());
                _incrBuffer.SetLength(0);
            }
        }

    }
}
