using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Clipboard.DataTypes
{
    /// <summary>
    /// Represents a virtual file structure that can be written to a dataobject
    /// </summary>
    [Serializable]
    public class FileAttributes
    {
        public override string ToString()
        {
            return FileName;
        }

        public FileAttributes(FileInfo info)
        {
            FileName = info.Name;
            FileSize = info.Length;
            LastChangeTime = info.LastWriteTime;
            FullPath = info.FullName;
            FileRequestId = Guid.NewGuid();
        }
        public string RelativePath { get; set; } = "";
        public string FileName { get; }
        public long FileSize { get; }
        public DateTime LastChangeTime { get; }

        [field: NonSerialized]
        public string FullPath { get; }
        public Guid FileRequestId { get; }

        /// <summary>
        /// Closes the IStream associated with this virtual file
        /// </summary>
        public void CloseStream()
        {
            CloseStreamRequested?.Invoke(this, null);
        }

        [field: NonSerialized]
        public event EventHandler CloseStreamRequested;
    }
}
