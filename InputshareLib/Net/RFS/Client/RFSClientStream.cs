using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib.Net.RFS.Client
{
    internal class RFSClientStream : Stream
    {
        private RFSClientFileGroup _group;
        private RFSFileHeader _file;
        private SocketBase _host => _group.Host;
        private RFSToken _token => _group.Token;

        internal RFSClientStream(RFSClientFileGroup group, RFSFileHeader file)
        {
            _group = group;
            _file = file;

            if (_group.Token == default)
                throw new RFSException("Group token not set");
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _file.FileSize;
        public override long Position { get; set; }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var reply = await _host.SendRequestAsync<RFSReadReply>(new RFSReadRequest(_token.Id, _group.GroupId, _file.FileId, count));
            Buffer.BlockCopy(reply.ReturnData, 0, buffer, 0, reply.ReturnData.Length);
            return reply.ReturnData.Length;
        }
    }
}
