using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.RFS.Client
{
    internal class RFSClientFileGroup : RFSFileGroup
    {
        internal RFSClientFileGroup(Guid groupId, RFSFileHeader[] files, SocketBase host) : base(groupId, files)
        {
            Host = host;
        }

        internal static RFSClientFileGroup FromGroup(RFSFileGroup group, SocketBase host)
        {
            return new RFSClientFileGroup(group.GroupId, group.Files, host);
        }

        internal RFSToken Token { get; set; }
        internal SocketBase Host { get; }
    }
}
