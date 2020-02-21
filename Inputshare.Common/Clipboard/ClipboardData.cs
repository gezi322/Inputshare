using Inputshare.Common.Net.RFS;
using Inputshare.Common.Net.RFS.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Inputshare.Common.Clipboard
{
    [Serializable]
    public class ClipboardData
    {
        internal ClipboardDataType[] AvailableTypes => _availableTypes.ToArray();
        private List<ClipboardDataType> _availableTypes = new List<ClipboardDataType>();

        private RFSFileGroup _fileGroup;
        private string[] _localFiles;
        private string _text;
        private byte[] _serializedBitmap;

        internal void SetBitmap(byte[] serializedBitmap)
        {
            _serializedBitmap = serializedBitmap;
            SetTypeAvailable(ClipboardDataType.Bitmap);
        }

        internal byte[] GetBitmapSerialized()
        {
            return _serializedBitmap;
        }

        internal Bitmap GetBitmap()
        {
            using (MemoryStream ms = new MemoryStream(_serializedBitmap))
            {
                return (Bitmap)Bitmap.FromStream(ms);
            }
        }

        internal void SetText(string text)
        {
            _text = text;
            SetTypeAvailable(ClipboardDataType.UnicodeText);
        }

        internal string GetText()
        {
            return _text;
        }

        internal void SetRemoteFiles(RFSFileGroup files)
        {
            _fileGroup = files;
            SetTypeAvailable(ClipboardDataType.RemoteFileGroup);
        }

        internal RFSFileGroup GetRemoteFiles()
        {
            return _fileGroup;
        }

        internal bool IsTypeAvailable(ClipboardDataType type)
        {
            return _availableTypes.Contains(type);
        }
        
        private void SetTypeAvailable(ClipboardDataType type)
        {
            if (!_availableTypes.Contains(type))
                _availableTypes.Add(type);
        }

        internal void SetLocalFiles(string[] sources)
        {
            _localFiles = sources;
            SetTypeAvailable(ClipboardDataType.HostFileGroup);
        }

        internal string[] GetLocalFiles()
        {
            return _localFiles;
        }
    }
}
