using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Common.Net.RFS
{
    /// <summary>
    /// Contains a list of file headers
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
