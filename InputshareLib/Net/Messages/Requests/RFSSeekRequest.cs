using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InputshareLib.Net.Messages.Requests
{
    [Serializable]
    internal class RFSSeekRequest : NetRequestBase
    {
        public RFSSeekRequest(Guid tokenId, Guid groupId, Guid fileId, SeekOrigin origin, long offset)
        {
            TokenId = tokenId;
            GroupId = groupId;
            FileId = fileId;
            Origin = origin;
            Offset = offset;
        }

        public Guid TokenId { get; }
        public Guid GroupId { get; }
        public Guid FileId { get; }
        public SeekOrigin Origin { get; }
        public long Offset { get; }
    }
}
