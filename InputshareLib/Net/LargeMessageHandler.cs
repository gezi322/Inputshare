using System;
using System.IO;

namespace InputshareLib.Net
{
    /// <summary>
    /// Acts as a buffer for a large packet of data that is beng received
    /// </summary>
    internal class LargeMessageHandler
    {
        public Guid MessageId { get; }
        public int MessageSize { get; }
        public int BytesRead { get; private set; }

        public bool FullyReceived { get => MessageSize == BytesRead; }

        private readonly MemoryStream messageStream;
        private bool cancelled;

        public LargeMessageHandler(Guid messageId, int messageSize)
        {
            messageStream = new MemoryStream();
            MessageId = messageId;
            MessageSize = messageSize;
        }
        public void Write(byte[] data)
        {
            if (cancelled)
                throw new InvalidOperationException("Handler has been cancelled");

            messageStream.Write(data, 0, data.Length);
            BytesRead += data.Length;
        }

        public void Close()
        {
            messageStream.Dispose();
        }

        public byte[] ReadAndClose()
        {
            if (!FullyReceived)
                throw new InvalidOperationException("Cannot read data until message has been fully received");

            messageStream.Seek(0, SeekOrigin.Begin);
            byte[] data = messageStream.ToArray();
            Cancel();
            return data;
        }

        public void Cancel()
        {
            if (cancelled)
                throw new InvalidOperationException("Handler has been cancelled");

            messageStream.Dispose();
            cancelled = true;
        }
    }
}
