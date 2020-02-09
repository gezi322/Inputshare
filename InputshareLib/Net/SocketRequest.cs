using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib.Net
{
    /// <summary>
    /// Represents a network request that is awaiting a reply
    /// </summary>
    internal class SocketRequest
    {
        public NetRequestBase RequestMessage { get; }
        private readonly SemaphoreSlim _semaphore;
        private NetReplyBase _reply;

        internal SocketRequest(NetRequestBase request)
        {
            RequestMessage = request;
            _semaphore = new SemaphoreSlim(0, 1);
        }

        /// <summary>
        /// Sets the response message and releases the wait handle
        /// </summary>
        /// <param name="reply"></param>
        internal void SetReplyMessage(NetReplyBase reply)
        {
            _reply = reply;
            _semaphore.Release();
        }

        /// <summary>
        /// Waits for a reply to the request
        /// </summary>
        /// <returns></returns>
        internal async Task<NetReplyBase> AwaitReply()
        {
            if (!await _semaphore.WaitAsync(5000))
                throw new NetRequestTimedOutException();

            return _reply;
        }
    }
}
