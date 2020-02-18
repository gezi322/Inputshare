using InputshareLib.Net.RFS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InputshareLib.Clipboard
{
    [Serializable]
    public class ClipboardData
    {
        internal ClipboardDataType[] AvailableTypes => _availableTypes.ToArray();
        private List<ClipboardDataType> _availableTypes = new List<ClipboardDataType>();

        private RFSFileGroup _remoteFiles;
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
            _remoteFiles = files;
            SetTypeAvailable(ClipboardDataType.RemoteFileGroup);
        }

        internal RFSFileGroup GetRemoteFiles()
        {
            return _remoteFiles;
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

    }
}
