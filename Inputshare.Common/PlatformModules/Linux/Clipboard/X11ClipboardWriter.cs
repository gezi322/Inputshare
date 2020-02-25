using Inputshare.Common.Clipboard;
using System;
using System.Collections.Generic;
using System.Text;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11Structs;

namespace Inputshare.Common.PlatformModules.Linux.Clipboard
{
    /// <summary>
    /// Handles selection transfers to other windows
    /// </summary>
    internal class X11ClipboardWriter
    {
        private IntPtr _xDisplay;
        private IntPtr _xWindow;
        private X11AtomList _atoms;
        private ClipboardData _cbData;

        internal X11ClipboardWriter(IntPtr xDisplay, IntPtr window, X11AtomList atoms)
        {
            _xDisplay = xDisplay;
            _xWindow = window;
            _atoms = atoms;
        }

        /// <summary>
        /// Sets this window as the clipboard selection owner
        /// </summary>
        /// <param name="cbData"></param>
        internal void SetClipboard(ClipboardData cbData)
        {
            _cbData = cbData;

            XSetSelectionOwner(_xDisplay, _atoms.Clipboard, _xWindow);

            if (XGetSelectionOwner(_xDisplay, _atoms.Clipboard) != _xWindow)
                throw new X11Exception("Failed to set clipboard selection owner");
        }

        /// <summary>
        /// Returns the compatible clipboard format to the window that requested a list of
        /// targets (todo - support more than one target)
        /// </summary>
        /// <param name="evt"></param>
        internal void ReturnCompatibleTargets(XSelectionRequestEvent evt)
        {
            if (_cbData == null)
                throw new InvalidOperationException("Clipboard data has not been set");

            byte[] rawData = new byte[0];
            if (_cbData.IsTypeAvailable(ClipboardDataType.UnicodeText))
                rawData = GetAtomsBytesText();
            else if (_cbData.IsTypeAvailable(ClipboardDataType.Bitmap))
                rawData = GetAtomsBytesImage();
            else
                Logger.Warning("Returning 0 compatible targets");

            XChangeProperty(_xDisplay, evt.requestor, evt.property, new IntPtr(4), 32, 0, rawData, 1);
        }

        /// <summary>
        /// Returns an image to the window that requested it
        /// </summary>
        /// <param name="evt"></param>
        internal void ReturnImage(XSelectionRequestEvent evt)
        {
            if (!_cbData.IsTypeAvailable(ClipboardDataType.Bitmap))
                throw new InvalidOperationException("No bitmap data has been set");

            var dataArray = _cbData.GetBitmapSerialized();
            XChangeProperty(_xDisplay, evt.requestor, evt.property, _atoms.ImagePng, 8, 0, dataArray, dataArray.Length);
        }

        /// <summary>
        /// Returns a UTF8 string to the window that requested text
        /// </summary>
        /// <param name="evt"></param>
        internal void ReturnText(XSelectionRequestEvent evt)
        {
            if (!_cbData.IsTypeAvailable(ClipboardDataType.UnicodeText))
                throw new InvalidOperationException("No text data has been set");

            string text = _cbData.GetText();
            XChangeProperty(_xDisplay, evt.requestor, evt.property, _atoms.Utf8String, 8, 0, Encoding.UTF8.GetBytes(text), text.Length);
        }


        private byte[] GetAtomsBytesText()
        {
            long[] allowedAtoms = new long[]
            {
                (long)_atoms.String,
                (long)_atoms.Text,
                (long)_atoms.Utf8String,
            };

            return LongArrayToByteArray(allowedAtoms);
        }

        private byte[] GetAtomsBytesImage()
        {
            long[] allowedAtoms = new long[]
            {
                (long)_atoms.ImagePng
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
    }
}
