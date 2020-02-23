﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Inputshare.Common.Net.RFS.Host
{
    /// <summary>
    /// Represents an instance of a file group access by a token
    /// </summary>
    internal class RFSGroupStreamInstance : IDisposable
    {
        internal event EventHandler<RFSGroupStreamInstance> Closed;
        internal Guid TokenId { get; }

        private Dictionary<Guid, FileStream> _streams = new Dictionary<Guid, FileStream>();
        private RFSHostFileGroup _group;
        private Timer _timeoutTimer;
        private int _timeout;

        /// <summary>
        /// Creates a stream instance of a filegroup
        /// </summary>
        /// <param name="group"></param>
        /// <param name="timeout"></param>
        internal RFSGroupStreamInstance(RFSHostFileGroup group, Guid tokenId, int timeout = 5000)
        {
            _timeout = timeout;
            TokenId = tokenId;
            _group = group;
            _timeoutTimer = new Timer();
            _timeoutTimer.Interval = 1500;
            _timeoutTimer.Elapsed += OnTimeoutTimerElapsed;
        }

        private void OnTimeoutTimerElapsed (object sender, ElapsedEventArgs e)
        {
            var lastReadMS = (DateTime.Now - e.SignalTime).TotalMilliseconds;

            if(lastReadMS > _timeout)
            {
                foreach (var stream in _streams)
                    stream.Value.Dispose();

                Closed?.Invoke(this, this);
                _timeoutTimer.Dispose();
                Logger.Verbose("Token timed out!");
            }
        }

        internal async Task<int> ReadAsync(Guid file, byte[] buffer, int readLen)
        {
            _timeoutTimer.Stop();
            _timeoutTimer.Start();

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
        internal int Read(Guid file, byte[] buffer, int readLen)
        {
            if (_streams.TryGetValue(file, out var stream))
            {
                return stream.Read(buffer, 0, readLen);
            }
            else
            {
                var newStream = InitStream(file);
                _streams.Add(file, newStream);
                return newStream.Read(buffer, 0, readLen);
            }
        }

        internal long Seek(Guid fileId, SeekOrigin origin, long offset)
        {
            _timeoutTimer.Stop();
            _timeoutTimer.Start();

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
                    _timeoutTimer?.Dispose();

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
