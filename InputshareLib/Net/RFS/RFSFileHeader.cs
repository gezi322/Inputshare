using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.RFS
{
    /// <summary>
    /// Contains information about a remote file
    /// </summary>
    [Serializable]
    internal class RFSFileHeader
    {
        internal Guid FileId { get; }
        internal string FileName { get; }
        public long FileSize { get; }
        public string RelativePath { get; }
        
        [field:NonSerialized]
        public string HostPath { get; }

        internal RFSFileHeader(Guid fileId, string fileName, long fileSize, string relativePath, string hostPath)
        {
            FileId = fileId;
            FileName = fileName;
            FileSize = fileSize;
            RelativePath = relativePath;
            HostPath = hostPath;
        }
    }
}
