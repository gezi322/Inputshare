using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.FileController
{
    internal interface IFileAccessToken
    {
        public Guid TokenId { get; }
        public event EventHandler<Guid> TokenClosed;

        public void SetTimeout(int ms);
        public void CloseAllStreams();
        public void CloseStream(Guid streamId);
        public Task<int> ReadFile(Guid file, byte[] buffer, int offset, int readLen);
    }
}
