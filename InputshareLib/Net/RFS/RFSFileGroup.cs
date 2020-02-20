using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Net.RFS
{
    /// <summary>
    /// A group of remote files
    /// </summary>
    [Serializable]
    internal class RFSFileGroup
    {
        [field:NonSerialized]
        internal bool RemoveOnIdle { get; set; }

        [field:NonSerialized]
        internal virtual event EventHandler<RFSFileGroup> TransfersFinished;

        internal RFSFileGroup(Guid groupId, RFSFileHeader[] files)
        {
            GroupId = groupId;
            Files = files;
        }

        internal virtual Task<Guid> GetTokenAsync()
        {
            throw new NotSupportedException();
        }
        internal virtual Task<int> ReadAsync(Guid tokenId, Guid fileId, byte[] buffer, int readLen)
        {
            throw new NotSupportedException();
        }

        internal virtual long Seek(Guid tokenId, Guid fileId, SeekOrigin origin, long offset)
        {
            throw new NotSupportedException();
        }

        public Guid GroupId { get; }
        public RFSFileHeader[] Files { get; }
    }
}
