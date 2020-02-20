using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Net.RFS.Client
{
    /// <summary>
    /// A group of remote files that can be streamed
    /// </summary>
    [Serializable]
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

        internal RFSClientStream CreateStream(RFSFileHeader header, Guid token)
        {
            return new RFSClientStream(this, header, token);
        }

        internal override async Task<Guid> GetTokenAsync()
        {
            var reply = await Host.SendRequestAsync<RFSTokenReply>(new RFSTokenRequest(GroupId));
            return reply.TokenId;
        }

        internal override async Task<int> ReadAsync(Guid tokenId, Guid fileId, byte[] buffer, int readLen)
        {
            var reply = await Host.SendRequestAsync<RFSReadReply>(new RFSReadRequest(tokenId, GroupId, fileId, readLen));
            Buffer.BlockCopy(reply.ReturnData, 0, buffer, 0, reply.ReturnData.Length);
            return reply.ReturnData.Length;
        }

        internal override long Seek(Guid tokenId, Guid fileId, SeekOrigin origin, long offset)
        {
            var reply = Host.SendRequest<RFSSeekReply>(new RFSSeekRequest(tokenId, GroupId, fileId, origin, offset));
            return reply.Position;
        }



        [field:NonSerialized]
        internal SocketBase Host { get; }
    }
}
