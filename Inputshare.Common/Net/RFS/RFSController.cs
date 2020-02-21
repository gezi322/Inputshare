using Inputshare.Common.Net.Messages;
using Inputshare.Common.Net.Messages.Replies;
using Inputshare.Common.Net.Messages.Requests;
using Inputshare.Common.Net.RFS.Client;
using Inputshare.Common.Net.RFS.Host;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inputshare.Common.Net.RFS
{
    /// <summary>
    /// Controls access to file groups hosted on the server or other clients
    /// </summary>
    internal class RFSController : IDisposable
    {
        /// <summary>
        /// Local files that are currently being hosted
        /// </summary>
        internal Dictionary<Guid, RFSHostFileGroup> HostedGroups = new Dictionary<Guid, RFSHostFileGroup>();
        
        /// <summary>
        /// File groups that are hosted on clients. This is used for client->client transfers
        /// </summary>
        internal Dictionary<Guid, RFSClientFileGroup> RemoteGroups = new Dictionary<Guid, RFSClientFileGroup>();

        private Thread _messageHandleThread;
        private BlockingCollection<RFSMessage> _messageQueue = new BlockingCollection<RFSMessage>();
        private CancellationTokenSource _cancelSource = new CancellationTokenSource();
       
        internal RFSController()
        {
            _messageHandleThread = new Thread(WorkerThreadLoop);
            _messageHandleThread.Start();
        }

        /// <summary>
        /// Posts a message to the RFS worker thread that handles the message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sender"></param>
        internal void HandleMessage(NetMessageBase message, SocketBase sender)
        {
            _messageQueue.Add(new RFSMessage(sender, message));
        }

        private void WorkerThreadLoop()
        {
            while (!disposedValue)
            {
                try
                {
                    var rfsMsg = _messageQueue.Take(_cancelSource.Token);

                    if (rfsMsg.Message is RFSTokenRequest tokenRequest)
                        HandleTokenRequest(tokenRequest, rfsMsg.Sender);
                    else if (rfsMsg.Message is RFSReadRequest readRequest)
                        HandleReadRequest(readRequest, rfsMsg.Sender);
                    else if (rfsMsg.Message is RFSSeekRequest seekRequest)
                        HandleSeekRequest(seekRequest, rfsMsg.Sender);

                }
                catch (Exception ex)
                {
                    Logger.Write("Failed to handle RFS message: " + ex.Message);
                }
            }
        }

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

        private void HandleTokenRequest(RFSTokenRequest request, SocketBase sender)
        {
            var group = GetGroup(request.GroupId);
            Guid tokenId = group.GetToken();
            sender.SendMessage(new RFSTokenReply(tokenId, request.MessageId));
        }

        private void HandleSeekRequest(RFSSeekRequest request, SocketBase sender)
        {
            var group = GetGroup(request.GroupId);
            long newPos = group.Seek(request.TokenId, request.FileId, request.Origin, request.Offset);
            sender.SendMessage(new RFSSeekReply(request.MessageId, newPos));
        }

        private void HandleReadRequest(RFSReadRequest request, SocketBase sender)
        {
            var group = GetGroup(request.GroupId);
            var data = ReadFromGroupFile(group, request.FileId, request.TokenId, request.ReadLen);
            sender.SendMessage(new RFSReadReply(request.MessageId, data));
        }

        /// <summary>
        /// Reads data from a file in the specified group
        /// </summary>
        /// <param name="group"></param>
        /// <param name="fileId"></param>
        /// <param name="tokenId"></param>
        /// <param name="readLen"></param>
        /// <returns></returns>
        private byte[] ReadFromGroupFile(RFSFileGroup group, Guid fileId, Guid tokenId, int readLen)
        {
            byte[] buff = new byte[readLen];
            int bRead = group.Read(tokenId, fileId, buff, readLen);

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

        private class RFSMessage
        {
            public RFSMessage(SocketBase sender, NetMessageBase message)
            {
                Sender = sender;
                Message = message;
            }

            public SocketBase Sender { get; }
            public NetMessageBase Message { get; }
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cancelSource?.Cancel();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
