using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Common.Net.RFS.Host
{
    /// <summary>
    /// Represents a file group that is being hosted.
    /// </summary>
    internal class RFSHostFileGroup : RFSFileGroup, IDisposable
    {
        internal override event EventHandler<RFSFileGroup> TransfersFinished;
        internal RFSFileHeader[] SourceFiles;
        internal Dictionary<Guid, RFSGroupStreamInstance> TokenInstances = new Dictionary<Guid, RFSGroupStreamInstance>();

        internal RFSHostFileGroup(Guid groupId, RFSFileHeader[] files) : base(groupId, files)
        {
            SourceFiles = files;
        }

        private Guid CreateToken()
        {
            Guid token = Guid.NewGuid();
            var instance = new RFSGroupStreamInstance(this, token, 5000);
            instance.Closed += OnStreamInstanceClosed;
            TokenInstances.Add(token, instance);
            return token;
        }

        private void OnStreamInstanceClosed(object sender, RFSGroupStreamInstance instance)
        {
            if (TokenInstances.ContainsKey(instance.TokenId))
            {
                TokenInstances.Remove(instance.TokenId);
            }

            if (TokenInstances.Count == 0 && RemoveOnIdle)
                TransfersFinished?.Invoke(this, this);
        }

        /// <summary>
        /// Read the specified file with the specified token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="file"></param>
        /// <param name="buffer"></param>
        /// <param name="readLen"></param>
        /// <returns></returns>
        internal override async Task<int> ReadAsync(Guid tokenId, Guid fileId, byte[] buffer, int readLen)
        {
            if(TokenInstances.TryGetValue(tokenId, out var tokenInstance))
            {
                return await tokenInstance.ReadAsync(fileId, buffer, readLen);
            }
            else
            {
                throw new RFSException("Token not found");
            }
        }

        internal override long Seek(Guid tokenId, Guid fileId, SeekOrigin origin, long offset)
        {
            if (TokenInstances.TryGetValue(tokenId, out var tokenInstance))
            {
                return tokenInstance.Seek(fileId, origin, offset);
            }
            else
            {
                throw new RFSException("Token not found");
            }
        }

        internal override Task<Guid> GetTokenAsync()
        {
            Guid id = CreateToken();
            return Task.FromResult(id);
        }

        internal override Guid GetToken()
        {
            return CreateToken();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var tokenInstance in TokenInstances)
                        tokenInstance.Value.Dispose();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }

        
        #endregion
    }
}
