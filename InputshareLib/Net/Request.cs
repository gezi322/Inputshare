using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib.Net
{
    internal class Request
    {
        public NetRequestBase RequestMessage { get; }
        public readonly Type ExpectedReply;
        private readonly SemaphoreSlim _semaphore;
        private NetReplyBase _reply;

        internal Request(NetRequestBase request, Type expectedReply)
        {
            ExpectedReply = expectedReply;
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

        internal async Task<NetReplyBase> AwaitReply()
        {
            if (!await _semaphore.WaitAsync(5000))
                throw new NetRequestTimedOutException();

            return _reply;
        }
    }
}
