using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.RFS
{
    /// <summary>
    /// A token that can be used to read data from a remote file
    /// </summary>
    [Serializable]
    internal class RFSToken
    {
        internal RFSToken(Guid tokenId)
        {
            Id = tokenId;
        }

        public Guid Id { get; }
    }
}
