using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages.Requests
{
    [Serializable]
    internal class RFSTokenRequest : NetRequestBase
    {
        internal RFSTokenRequest(Guid groupId)
        {
            GroupId = groupId;
        }

        public Guid GroupId { get; }
    }
}
