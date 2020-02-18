using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Net.RFS.Host
{
    /// <summary>
    /// Represents a file group that is being hosted.
    /// </summary>
    internal class RFSHostFileGroup : RFSFileGroup, IDisposable
    {
        internal RFSHostFileHeader[] SourceFiles;
        private Dictionary<Guid, RFSGroupStreamInstance> _tokenInstances = new Dictionary<Guid, RFSGroupStreamInstance>();

        internal RFSHostFileGroup(Guid groupId, RFSHostFileHeader[] files) : base(groupId, files)
        {
            SourceFiles = files;
        }

        internal RFSToken CreateToken()
        {
            RFSToken token = new RFSToken(Guid.NewGuid());

            _tokenInstances.Add(token.Id, new RFSGroupStreamInstance(this));
            return token;
        }

        /// <summary>
        /// Read the specified file with the specified token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="file"></param>
        /// <param name="buffer"></param>
        /// <param name="readLen"></param>
        /// <returns></returns>
        internal async Task<int> ReadAsync(Guid tokenId, Guid fileId, byte[] buffer, int readLen)
        {
            if(_tokenInstances.TryGetValue(tokenId, out var tokenInstance))
            {
                return await tokenInstance.ReadAsync(fileId, buffer, readLen);
            }
            else
            {
                throw new RFSException("Token not found");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var tokenInstance in _tokenInstances)
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
