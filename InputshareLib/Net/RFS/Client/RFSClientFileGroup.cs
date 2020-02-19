﻿using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using System;
using System.Collections.Generic;
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

        internal RFSClientStream CreateStream(RFSFileHeader header, RFSToken token)
        {
            return new RFSClientStream(this, header, token);
        }

        internal async Task<int> ReadAsync(RFSToken token, RFSFileHeader header, byte[] buffer, int readLen)
        {
            var reply = await Host.SendRequestAsync<RFSReadReply>(new RFSReadRequest(token.Id, GroupId, header.FileId, readLen));
            Buffer.BlockCopy(reply.ReturnData, 0, buffer, 0, reply.ReturnData.Length);
            return reply.ReturnData.Length;
        }

        internal async Task<RFSToken> GetTokenAsync()
        {
            var reply = await Host.SendRequestAsync<RFSTokenReply>(new RFSTokenRequest(GroupId));
            return new RFSToken(reply.TokenId);
        }


        [field:NonSerialized]
        internal SocketBase Host { get; }
    }
}
