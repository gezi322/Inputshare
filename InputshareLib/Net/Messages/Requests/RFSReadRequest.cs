using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages.Requests
{
    [Serializable]
    internal class RFSReadRequest : NetRequestBase
    {
        public RFSReadRequest(Guid tokenId, Guid groupId, Guid fileId, int readLen)
        {
            TokenId = tokenId;
            GroupId = groupId;
            FileId = fileId;
            ReadLen = readLen;
        }

        public Guid TokenId { get; }
        public Guid GroupId { get; }
        public Guid FileId { get; }
        public int ReadLen { get; }
    }
}
