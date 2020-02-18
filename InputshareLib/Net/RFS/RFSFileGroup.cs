using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.RFS
{
    /// <summary>
    /// A group of remote files
    /// </summary>
    [Serializable]
    internal class RFSFileGroup
    {
        internal RFSFileGroup(Guid groupId, RFSFileHeader[] files)
        {
            GroupId = groupId;
            Files = files;
        }

        public Guid GroupId { get; }
        public RFSFileHeader[] Files { get; }
    }
}
