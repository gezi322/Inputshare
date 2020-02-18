using InputshareLib.Net.Messages;
using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using InputshareLib.Net.RFS.Client;
using InputshareLib.Net.RFS.Host;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Net.RFS
{
    internal class RFSController
    {
        private Dictionary<Guid, RFSHostFileGroup> _hostedGroups = new Dictionary<Guid, RFSHostFileGroup>();

        internal RFSController()
        {

        }

        /// <summary>
        /// Hosts the specified files 
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        internal RFSFileGroup HostFiles(string[] sources)
        {
            RFSHostFileHeader[] files = new RFSHostFileHeader[sources.Length];
            RFSFileHeader[] standardFiles = new RFSFileHeader[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                files[i] = new RFSHostFileHeader(new System.IO.FileInfo(sources[i]));
                standardFiles[i] = new RFSFileHeader(files[i].FileId, files[i].FileName, files[i].FileSize);
            }

            var group = new RFSHostFileGroup(Guid.NewGuid(), files);
            _hostedGroups.Add(group.GroupId, group);
            return new RFSFileGroup(group.GroupId, standardFiles);
        }

        /// <summary>
        /// Gets a token to get an instance of a file from the specified file group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private async Task<RFSToken> GetTokenAsync(RFSClientFileGroup group)
        {
            var reply = await group.Host.SendRequestAsync<RFSTokenReply>(new RFSTokenRequest(group.GroupId));
            return new RFSToken(reply.TokenId);
        }

        /// <summary>
        /// Creates a stream of the remote file
        /// </summary>
        /// <param name="group"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        internal async Task<RFSClientStream> CreateStreamAsync(RFSClientFileGroup group, RFSFileHeader header)
        {
            if (group.Token == default)
                group.Token = await GetTokenAsync(group);

            return new RFSClientStream(group, header);
        }

        private async Task<byte[]> ReadHostedFile(Guid tokenId, Guid groupId, Guid fileId, int readLen)
        {
            if(_hostedGroups.TryGetValue(groupId, out var group))
            {
                byte[] buff = new byte[readLen];
                int rLen = await group.ReadAsync(tokenId, fileId, buff, readLen);

                //Make sure the returned array is the correct size
                if(rLen != buff.Length)
                {
                    byte[] resizedBuff = new byte[rLen];
                    Buffer.BlockCopy(buff, 0, resizedBuff, 0, rLen);
                    buff = resizedBuff;
                }

                return buff;
            }
            else
            {
                throw new RFSException("Group not found");
            }
        }

        internal async Task HandleNetMessageAsync(NetMessageBase message, SocketBase sender)
        {
            if (message is RFSReadRequest readRequest)
                await HandleReadRequest(readRequest, sender);
            else if (message is RFSTokenRequest tokenRequest)
                await HandleTokenRequest(tokenRequest, sender);
        }

        private async Task HandleTokenRequest(RFSTokenRequest request, SocketBase sender)
        {
            if(_hostedGroups.TryGetValue(request.GroupId, out var group))
            {
                var token = group.CreateToken();
                await sender.SendMessageAsync(new RFSTokenReply(token.Id, request.MessageId));
            }
            else
            {
                throw new RFSException("Group ID not found");
            }
        }

        private async Task HandleReadRequest(RFSReadRequest readRequest, SocketBase sender)
        {
            byte[] data = await ReadHostedFile(readRequest.TokenId, readRequest.GroupId, readRequest.FileId, readRequest.ReadLen);
            await sender.SendMessageAsync(new RFSReadReply(readRequest.MessageId, data));
        }
    }
}
