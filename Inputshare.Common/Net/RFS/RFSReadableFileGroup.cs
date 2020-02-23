using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Common.Net.RFS
{
    /// <summary>
    /// A group of files that can be streamed
    /// </summary>
    internal abstract class RFSReadableFileGroup : RFSFileGroup
    {
        [field: NonSerialized]
        internal virtual event EventHandler<RFSFileGroup> TransfersFinished;

        [field: NonSerialized]
        internal bool RemoveOnIdle { get; set; }

        internal RFSReadableFileGroup(Guid groupId, RFSFileHeader[] files) : base(groupId, files)
        {

        }

        internal abstract Task<Guid> GetTokenAsync();
        internal abstract Task<int> ReadAsync(Guid tokenId, Guid fileId, byte[] buffer, int readLen);

        internal abstract int Read(Guid tokenId, Guid fileId, byte[] buffer, int readLen);
        internal abstract Guid GetToken();

        internal abstract long Seek(Guid tokenId, Guid fileId, SeekOrigin origin, long offset);
    }
}
