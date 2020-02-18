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

        internal RFSFileHeader(Guid fileId, string fileName, long fileSize)
        {
            FileName = fileName;
            FileSize = fileSize;
        }
    }
}
