using InputshareLib.Net.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InputshareLib.Net
{
    /// <summary>
    /// Stores an incomplete segmented message
    /// </summary>
    internal class SegmentedMessageHandler : IDisposable
    {
        internal event EventHandler<NetMessageBase> MessageComplete;

        private MemoryStream _buffer;

        internal SegmentedMessageHandler(int messageSize)
        {
            _buffer = new MemoryStream(messageSize);
        }

        internal void Write(NetMessageSegment message)
        {
            _buffer.Write(message.Data, 0, message.Data.Length);
            Logger.Write($"{_buffer.Position}/{_buffer.Capacity}");

            if (_buffer.Position == _buffer.Capacity)
            {
                _buffer.Seek(0, SeekOrigin.Begin);
                var fullMessage = NetMessageSerializer.Deserialize<NetMessageBase>(_buffer);
                MessageComplete?.Invoke(this, fullMessage);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; 
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _buffer?.Dispose();
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
