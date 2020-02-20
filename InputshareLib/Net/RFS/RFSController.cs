using InputshareLib.Net.Messages;
using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using InputshareLib.Net.RFS.Client;
using InputshareLib.Net.RFS.Host;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Net.RFS
{
    /// <summary>
    /// Controls access to file groups hosted on the server or other clients
    /// </summary>
    internal class RFSController
    {
        /// <summary>
        /// Local files that are currently being hosted
        /// </summary>
        internal Dictionary<Guid, RFSHostFileGroup> HostedGroups = new Dictionary<Guid, RFSHostFileGroup>();
        
        /// <summary>
        /// File groups that are hosted on clients. This is used for client->client transfers
        /// </summary>
        internal Dictionary<Guid, RFSClientFileGroup> RemoteGroups = new Dictionary<Guid, RFSClientFileGroup>();

        /// <summary>
        /// Allows clients to access local files
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        internal RFSFileGroup HostLocalGroup(string[] sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            RFSFileHeader[] headers = FileStructureConverter.CreateFileHeaders(sources);
            var group = new RFSHostFileGroup(Guid.NewGuid(), headers);
            group.TransfersFinished += OnFileGroupFinished;
            HostedGroups.Add(group.GroupId, group);
            return new RFSFileGroup(group.GroupId, group.Files);
        }

        private void OnFileGroupFinished(object sender, RFSFileGroup e)
        {
            //When a file group is unhosted, and all transfers are finished
            //then remove the group from the dictionary
            if (HostedGroups.ContainsKey(e.GroupId))
                HostedGroups.Remove(e.GroupId);
            else if (RemoteGroups.ContainsKey(e.GroupId))
                RemoteGroups.Remove(e.GroupId);
        }

        /// <summary>
        /// Stops hosting a file group. Any transfers in progress will continue
        /// </summary>
        /// <param name="group"></param>
        internal void UnHostFiles(RFSFileGroup group)
        {
            if (HostedGroups.TryGetValue(group.GroupId, out var hostedGroup))
                if (hostedGroup.TokenInstances.Count == 0)
                    HostedGroups.Remove(group.GroupId);
                else
                    hostedGroup.RemoveOnIdle = true;

            //TODO - remove remote file groups, but not until we are sure there 
            //are no files being transfered
        }
        
        internal void HostRemoteGroup(RFSClientFileGroup group)
        {
            RemoteGroups.Add(group.GroupId, group);
            Logger.Write("Current hosted groups = " + HostedGroups.Count);
        }

        internal async Task HandleNetMessageAsync(NetMessageBase message, SocketBase sender)
        {
            try
            {
                if (message is RFSTokenRequest tokenRequest)
                    await HandleTokenRequestAsync(tokenRequest, sender);
                else if (message is RFSReadRequest readRequest)
                    await HandleReadRequestAsync(readRequest, sender);
                else if (message is RFSSeekRequest seekRequest)
                    await HandleSeekRequestAsync(seekRequest, sender);
            }
            catch(Exception ex)
            {
                Logger.Write($"RFSController -> Failed to handle request {message.GetType().Name}: {ex.Message}");
            }
        }

        private async Task HandleTokenRequestAsync(RFSTokenRequest request, SocketBase sender)
        {
            var group = GetGroup(request.GroupId);
            Guid tokenId = await group.GetTokenAsync();
            await sender.SendMessageAsync(new RFSTokenReply(tokenId, request.MessageId));
        }

        private async Task HandleSeekRequestAsync(RFSSeekRequest request, SocketBase sender)
        {
            var group = GetGroup(request.GroupId);
            long newPos = group.Seek(request.TokenId, request.FileId, request.Origin, request.Offset);
            await sender.SendMessageAsync(new RFSSeekReply(request.MessageId, newPos));
        }

        private async Task HandleReadRequestAsync(RFSReadRequest request, SocketBase sender)
        {
            var group = GetGroup(request.GroupId);
            var data = await ReadFromGroupFileAsync(group, request.FileId, request.TokenId, request.ReadLen);
            await sender.SendMessageAsync(new RFSReadReply(request.MessageId, data));
        }

        /// <summary>
        /// Reads data from a file in the specified group
        /// </summary>
        /// <param name="group"></param>
        /// <param name="fileId"></param>
        /// <param name="tokenId"></param>
        /// <param name="readLen"></param>
        /// <returns></returns>
        private async Task<byte[]> ReadFromGroupFileAsync(RFSFileGroup group, Guid fileId, Guid tokenId, int readLen)
        {
            byte[] buff = new byte[readLen];
            int bRead = await group.ReadAsync(tokenId, fileId, buff, readLen);

            //Make sure the returned buffer is the correct size so no random data is included
            if(buff.Length != bRead)
            {
                byte[] resizedBuff = new byte[bRead];
                Buffer.BlockCopy(buff, 0, resizedBuff, 0, bRead);
                buff = resizedBuff;
            }

            return buff;
        }  

        internal RFSFileGroup GetGroup(Guid group)
        {
            if (HostedGroups.TryGetValue(group, out var hostGroup))
                return hostGroup;
            if (RemoteGroups.TryGetValue(group, out var remoteGroup))
                return remoteGroup;

            throw new RFSException("GroupID not found");
        }

    }
}
