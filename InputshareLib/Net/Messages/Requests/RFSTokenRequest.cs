using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages.Requests
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
