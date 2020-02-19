﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Net.RFS.Host
{
    /// <summary>
    /// Represents an instance of a file group access by a token
    /// </summary>
    internal class RFSGroupStreamInstance : IDisposable
    {
        private Dictionary<Guid, FileStream> _streams = new Dictionary<Guid, FileStream>();
        private RFSHostFileGroup _group;

        internal RFSGroupStreamInstance(RFSHostFileGroup group)
        {
            _group = group;
        }

        internal async Task<int> ReadAsync(Guid file, byte[] buffer, int readLen)
        {
            if (_streams.TryGetValue(file, out var stream))
            {
                return await stream.ReadAsync(buffer, 0, readLen);
            }
            else
            {
                var newStream = InitStream(file);
                _streams.Add(file, newStream);
                return await newStream.ReadAsync(buffer, 0, readLen);
            }
        }

        internal long Seek(Guid fileId, SeekOrigin origin, long offset)
        {
            if (_streams.TryGetValue(fileId, out var stream))
            {
                return stream.Seek(offset, origin);
            }
            else
            {
                var newStream = InitStream(fileId);
                _streams.Add(fileId, newStream);
                return 0;
            }
        }

        private FileStream InitStream(Guid fileId)
        {
            var header = _group.SourceFiles.Where(i => i.FileId == fileId).FirstOrDefault();

            if (header == null)
                throw new RFSException("File ID not found");

            return File.OpenRead(header.HostPath);
        }


        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var stream in _streams)
                        stream.Value.Dispose();
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
