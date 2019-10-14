using InputshareLib;
using InputshareLibWindows.IPC.AnonIpc.Messages;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace InputshareLibWindows.IPC.AnonIpc
{
    public abstract class AnonIpcBase : IDisposable
    {
        public event EventHandler<Exception> PipeDisconnected;

        protected string pipeName { get; }
        protected PipeStream pipeWrite;
        protected PipeStream pipeRead;

        protected string writeHandle;
        protected string readHandle;
        private AnonymousPipeServerStream rawPipeWrite;
        private AnonymousPipeServerStream rawPipeRead;

        protected byte[] readBuffer = new byte[1024];

        public bool Connected { get; protected set; }
        private bool reCreating = false;

        protected Dictionary<Guid, Tuple<AutoResetEvent, AnonIpcMessage>> awaitingMessages = new Dictionary<Guid, Tuple<AutoResetEvent, AnonIpcMessage>>();
        protected object awaitingMessagesLock = new object();

        public AnonIpcBase(PipeStream write, PipeStream read, string conName)
        {
            pipeName = conName;
            pipeWrite = write;
            pipeRead = read;

            pipeRead.BeginRead(readBuffer, 0, 4, ReadCallback, null);
        }

        public AnonIpcBase(string writeHandle, string readHandle, string conName)
        {
            pipeWrite = new AnonymousPipeClientStream(PipeDirection.Out, writeHandle);
            pipeRead = new AnonymousPipeClientStream(PipeDirection.In, readHandle);
            pipeName = conName;
            pipeRead.BeginRead(readBuffer, 0, 4, ReadCallback, null);
        }

        public AnonIpcBase(string conName)
        {
            pipeName = conName;
            CreateHost();
        }

        protected void CreateHost()
        {
            Connected = false;
            reCreating = true;
            rawPipeWrite = new AnonymousPipeServerStream(PipeDirection.Out, System.IO.HandleInheritability.Inheritable);
            rawPipeRead = new AnonymousPipeServerStream(PipeDirection.In, System.IO.HandleInheritability.Inheritable);

            pipeWrite = rawPipeWrite;
            pipeRead = rawPipeRead;

            readHandle = rawPipeRead.GetClientHandleAsString();
            writeHandle = rawPipeWrite.GetClientHandleAsString();
            reCreating = false;
            pipeRead.BeginRead(readBuffer, 0, 4, ReadCallback, null);
        }

        /// <summary>
        /// Sends a request to the client and waits for a reply
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="TimeoutException"></exception>"
        /// <returns></returns>
        protected AnonIpcMessage SendRequest(AnonIpcMessage message)
        {
            AutoResetEvent evt = new AutoResetEvent(false);
            AnonIpcMessage returnedMessage;
            lock (awaitingMessagesLock)
            {
                awaitingMessages.Add(message.MessageId, new Tuple<AutoResetEvent, AnonIpcMessage>(evt, new AnonIpcMessage(AnonIpcMessageType.ClientOK)));
            }

            Write(message);
            bool cancelled = !evt.WaitOne(5000);

            lock (awaitingMessagesLock)
            {
                evt.Dispose();
                awaitingMessages.TryGetValue(message.MessageId, out Tuple<AutoResetEvent, AnonIpcMessage> ret);
                returnedMessage = ret.Item2;
                awaitingMessages.Remove(message.MessageId);
            }

            if (cancelled)
                throw new TimeoutException();

            return returnedMessage;
        }

        protected void DisposeHandles()
        {
            try
            {
                rawPipeRead.DisposeLocalCopyOfClientHandle();
                rawPipeWrite.DisposeLocalCopyOfClientHandle();
            }
            catch (Exception ex)
            {
                ISLogger.Write("AnonIpcBase (" + pipeName + ") : Failed to dispose local pipe handles: " + ex.Message);
            }
        }

        private int bytesIn;
        private void ReadCallback(IAsyncResult ar)
        {
            try
            {
                bytesIn = pipeRead.EndRead(ar);
                do
                {
                    bytesIn += pipeRead.Read(readBuffer, bytesIn, 4 - bytesIn);
                } while (bytesIn < 4);

                int header = BitConverter.ToInt32(readBuffer, 0);

                //TODO - temp
                if (header > readBuffer.Length)
                    readBuffer = new byte[header + 4];
                bytesIn = pipeRead.Read(readBuffer, 4, header);
                do
                {
                    bytesIn += pipeRead.Read(readBuffer, bytesIn + 4, header - bytesIn);
                } while (bytesIn < header);

                try
                {
                    byte[] data = new byte[bytesIn];
                    Buffer.BlockCopy(readBuffer, 4, data, 0, bytesIn);
                    PreProcessMessage(data);
                }
                catch (Exception ex)
                {
                    ISLogger.Write($"AnonIpcBase (" + pipeName + "): Unhandled exception while handling message: " + ex.Message);
                    ISLogger.Write(ex.StackTrace);
                }

                if (readBuffer.Length != 1024)
                    readBuffer = new byte[1024];

                pipeRead.BeginRead(readBuffer, 0, 4, ReadCallback, null);
            }
            catch (Exception ex)
            {
                if (Connected && !reCreating)
                {
                    ISLogger.Write("AnonIpcBase (" + pipeName + "): Pipe read error: " + ex.Message);
                    PipeDisconnected?.Invoke(this, ex);
                    ISLogger.Write(ex.StackTrace);
                    Connected = false;
                }
            }
        }

        private void PreProcessMessage(byte[] data)
        {
            bool handled = false;
            AnonIpcMessageType type = (AnonIpcMessageType)data[0];
            if(type != AnonIpcMessageType.InputData)
            {
                lock (awaitingMessagesLock)
                {
                    Guid id = data.ParseGuid(1);

                    if (awaitingMessages.TryGetValue(id, out Tuple<AutoResetEvent, AnonIpcMessage> awaiting))
                    {
                        AutoResetEvent evt = awaiting.Item1;
                        AnonIpcMessage msg = null;

                        if (type == AnonIpcMessageType.DisplayConfigReply)
                            msg = new AnonIpcDisplayConfigMessage(data);
                        if (type == AnonIpcMessageType.StreamReadResponse)
                            msg = new AnonIpcReadStreamResponseMessage(data);


                        awaitingMessages.Remove(id);
                        awaitingMessages.Add(id, new Tuple<AutoResetEvent, AnonIpcMessage>(evt, msg));
                        evt.Set();
                        handled = true;
                    }
                }
            }
            

            if (!handled)
                ProcessMessage(data);
        }

        protected abstract void ProcessMessage(byte[] data);

        protected void Write(AnonIpcMessage message)
        {
            Write(message.ToBytes());
        }

        protected void Write(AnonIpcMessageType message, Guid messageId = default)
        {
            Write(new AnonIpcMessage(message, messageId));
        }

        protected void Write(byte[] data)
        {
            try
            {
                byte[] fullPacket = new byte[data.Length + 4];
                Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, fullPacket, 0, 4);
                Buffer.BlockCopy(data, 0, fullPacket, 4, data.Length);
                pipeWrite.Write(fullPacket);
               
            }
            catch (Exception ex)
            {
                ISLogger.Write("AnonIpcBase (" + pipeName + "): Write error - " + ex.Message);
                PipeDisconnected?.Invoke(this, ex);
                Connected = false;
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
                    pipeWrite.Dispose();
                    pipeRead.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        public class RemoteStreamReadException : Exception
        {

        }

        public class InvalidResponseException : Exception
        {
            public InvalidResponseException() : base("The client sent an invalid response")
            {
            }
        }
    }
}
