using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using InputshareLib.Server;

namespace InputshareLib.FileController
{
    class RemoteFileAccessToken : IFileAccessToken
    {
        public Guid TokenId { get; }
        public ISServerSocket HostClient { get; }

        public event EventHandler<Guid> TokenClosed;

        public RemoteFileAccessToken(Guid tokenId, ISServerSocket host)
        {
            TokenId = tokenId;
            HostClient = host;

            host.ConnectionError += (object o, string e) => { TokenClosed?.Invoke(this, tokenId); };
        }

        public void CloseAllStreams()
        {

        }

        public void CloseStream(Guid streamId)
        {
            //Todo - client should notify server if it closes a token
        }

        public async Task<int> ReadFile(Guid file, byte[] buffer, int offset, int readLen)
        {
            byte[] data = await HostClient.RequestReadStreamAsync(TokenId, file, readLen);

            Buffer.BlockCopy(data, 0, buffer, offset, data.Length);
            return data.Length;
        }

        public void SetTimeout(int ms)
        {

        }
    }
}
