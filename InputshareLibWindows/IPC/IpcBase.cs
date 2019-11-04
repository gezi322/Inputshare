using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using InputshareLib;
using System.Threading;
using System.Threading.Tasks;
using InputshareLib.Input;
using System.IO.Pipes;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace InputshareLibWindows.IPC
{

    /// <summary>
    /// Base class for IPC methods.
    /// </summary>
    public abstract class IpcBase : IDisposable
    {
        /// <summary>
        /// Fired when the IPC is ready for read/write operations
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        /// Fired when the IPC loses connection
        /// </summary>
        public event EventHandler<string> Disconnected;

        /// <summary>
        /// Fired when IsInputData is received
        /// </summary>
        public event EventHandler<ISInputData> InputReceived;

        /// <summary>
        /// Set when the IPC is connected
        /// </summary>
        public AutoResetEvent ConnectedEvent { get; } = new AutoResetEvent(false);

        /// <summary>
        /// True if the IPC is ready to read/write data
        /// </summary>
        public virtual bool IsConnected { get; protected set; }

        protected Stream readStream;
        protected Stream writeStream;

        private byte[] readBuffer = new byte[1024];
        private bool useSerializer = true;

        /// <summary>
        /// Contains message GUIDs that are waiting for a reply
        /// </summary>
        private Dictionary<Guid, Tuple<SemaphoreSlim, IpcMessage>> awaitingResponseMessages = new Dictionary<Guid, Tuple<SemaphoreSlim, IpcMessage>>();
        private object awaitingResponseLock = new object();

        protected string connectionName;
        protected bool dedicatedWriteThread;

        //Fields used for a dedicated write thread
        private Task writeTask;
        private BlockingCollection<byte[]> writeQueue = new BlockingCollection<byte[]>();
        private CancellationTokenSource writeTaskCancel = new CancellationTokenSource();

        /// <summary>
        /// Base class for IPC operations
        /// </summary>
        /// <param name="serialize">If true, messages are serialized with the IpcSerializer, otherwise 
        /// any inherited IpcMessage will have to have a valid MyIpcMessage(byte[] data) constructor and ToBytes() implementation</param>
        /// <param name="conName">Name of the current connection for logging purposes</param>
        /// <param name="dedicatedThread">If true, A dedicated thread will be created to write data</param>
        public IpcBase(bool serialize, string conName, bool dedicatedThread)
        {
            connectionName = conName;
            useSerializer = serialize;
            dedicatedWriteThread = dedicatedThread;

            if (dedicatedWriteThread)
            {
                writeTask = new Task(WriteTaskLoop, writeTaskCancel.Token);
                writeTask.Start();
            }
        }

        protected void Start(Stream stream)
        {
            readStream = stream;
            writeStream = stream;
            readStream.BeginRead(readBuffer, 0, 4, ReadCallback, readStream);
        }
        protected void Start(Stream rStream, Stream wStream)
        {
            readStream = rStream;
            writeStream = wStream;
            readStream.BeginRead(readBuffer, 0, 4, ReadCallback, readStream);
        }

        /// <summary>
        /// Used if serialize is set to true in the IpcBase constructor
        /// </summary>
        private void WriteTaskLoop()
        {
            while (!writeTaskCancel.IsCancellationRequested)
            {
                try
                {
                    byte[] data = writeQueue.Take();
                    byte[] p = new byte[data.Length + 4];
                    Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, p, 0, 4);
                    Buffer.BlockCopy(data, 0, p, 4, data.Length);
                    writeStream.Write(p);
                }
                catch(Exception ex)
                {
                    OnError(ex);
                }
            }
        }


        private int bytesIn;
        private void ReadCallback(IAsyncResult ar)
        {
            Stream recv = (Stream)ar.AsyncState;

            try
            {
                bytesIn = recv.EndRead(ar);

                //Make sure we read all 4 bytes
                do
                {
                    bytesIn += recv.Read(readBuffer, bytesIn, 4 - bytesIn);
                } while (bytesIn < 4);

                int header = BitConverter.ToInt32(readBuffer, 0);
                
                //TODO - implement a better way to deal with messages larger than buffer
                if (header + 4 > readBuffer.Length && header < 1024*1024*50) //max 50mb
                    readBuffer = new byte[header + 4];

                bytesIn = recv.Read(readBuffer, 4, header);

                //Keep reading bytes until full packet has been read
                while(bytesIn != header)
                    bytesIn += recv.Read(readBuffer, bytesIn + 4, header - bytesIn);

                byte[] packet = new byte[header];
                Buffer.BlockCopy(readBuffer, 4, packet, 0, header);

                try
                {
                    if (!useSerializer)
                        PreProcessMessage(packet);
                    else
                        PreProcessMessage(IpcMessageSerializer.DeSerialize(packet));
                }catch(Exception ex)
                {
                    ISLogger.Write("IpcBase: An error occurred while handling message: " + ex.Message);
                    ISLogger.Write(ex.StackTrace);
                    Thread.Sleep(2000);
                    Process.GetCurrentProcess().Kill();
                }

                //Reset buffer size if changed
                if (readBuffer.Length != 1024)
                    readBuffer = new byte[1024];

                recv.BeginRead(readBuffer, 0, 4, ReadCallback, recv);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch(Exception ex)
            {
                OnError(ex);
            }
        }

        /// <summary>
        /// Sends a request to the IPC and waits for a response.
        /// </summary>
        /// <param name="request">Type of request</param>
        /// <param name="expectedReply">Expected type of reply</param>
        /// <param name="timeout">Timeout length (in MS)</param>
        /// <returns></returns>
        /// <exception cref="IpcInvalidReponseException>">The IPC sent an unexpected response</exception>"
        /// <exception cref="TimeoutException">The operation timed out</exception>"
        protected async Task<IpcMessage> SendRequest(IpcMessage request, IpcMessageType expectedReply, int timeout = 5000)
        {
            SemaphoreSlim evt = new SemaphoreSlim(0, 1);
            IpcMessage returnedMessage;
            lock (awaitingResponseLock)
            {
                awaitingResponseMessages.Add(request.MessageId, new Tuple<SemaphoreSlim, IpcMessage>(evt, null));
            }

            Write(request);
            bool cancelled = !await evt.WaitAsync(timeout);

            lock (awaitingResponseLock)
            {
                evt.Dispose();
                awaitingResponseMessages.TryGetValue(request.MessageId, out Tuple<SemaphoreSlim, IpcMessage> ret);
                returnedMessage = ret.Item2;
                awaitingResponseMessages.Remove(request.MessageId);
            }

            if (cancelled)
                throw new TimeoutException();

            if (returnedMessage.MessageType != expectedReply)
                throw new IpcInvalidReponseException(returnedMessage.MessageType, expectedReply);

            return returnedMessage;
        }

        //Preprocess non-serialized data and manage standard IPC messages
        private void PreProcessMessage(byte[] data)
        {
            IpcMessageType type = (IpcMessageType)data[0];

            if (type == IpcMessageType.AnonIpcInputData)
            {
                InputReceived?.Invoke(this, new ISInputData(data, 1));
                return;
            }else if(type == IpcMessageType.IpcPoll)
            {
                HandlePoll(new IpcMessage(data));
                return;
            }
            else if(type == IpcMessageType.IpcHostOK)
            {
                HandleHostOK();
                return;
            }else if(type == IpcMessageType.IpcClientOK)
            {
                HandleClientOK();
                return;
            }

            IpcMessage msg = null;
            if (type == IpcMessageType.AnonIpcClipboardData)
                msg = new AnonIpc.Messages.AnonIpcClipboardDataMessage(data);
            else if (type == IpcMessageType.AnonIpcDisplayConfigReply)
                msg = new AnonIpc.Messages.AnonIpcDisplayConfigMessage(data);
            else if (type == IpcMessageType.AnonIpcStreamReadResponse)
                msg = new AnonIpc.Messages.AnonIpcReadStreamResponseMessage(data);
            else if (type == IpcMessageType.AnonIpcDoDragDrop)
                msg = new AnonIpc.Messages.AnonIpcDoDragDropMessage(data);

            if(msg == null)
                msg = new IpcMessage(data);

            if (!CheckAwaitingMessages(msg))
                ProcessMessage(type, data);
        }

        //Preprocess serialized data and manage standard IPC messages
        private void PreProcessMessage(IpcMessage message)
        {
            if(message.MessageType == IpcMessageType.IpcPoll)
            {
                HandlePoll(message);
                return;
            }

            if (message.MessageType == IpcMessageType.IpcHostOK)
            {
                HandleHostOK();
                return;
            }
            else if (message.MessageType == IpcMessageType.IpcClientOK)
            {
                HandleClientOK();
                return;
            }


            if (!CheckAwaitingMessages(message))
                ProcessMessage(message);
        }

        private void HandlePoll(IpcMessage message)
        {
            Write(new IpcMessage(IpcMessageType.IpcPollResponse, message.MessageId));
        }

        private void HandleHostOK()
        {
            OnConnected();
        }

        private void HandleClientOK()
        {
            Write(new IpcMessage(IpcMessageType.IpcHostOK));
            OnConnected();
        }

        public void SendInput(ISInputData input)
        {
            byte[] data = new byte[6];
            data[0] = (byte)IpcMessageType.AnonIpcInputData;
            input.ToBytes(data, 1);
            Write(data);
        }

        

        /// <summary>
        /// Checks if the IpcMessage is a response to a request. If this method returns true,
        /// don't process the message as another thread is waiting to handle it.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool CheckAwaitingMessages(IpcMessage message)
        {
            lock (awaitingResponseLock)
            {
                if (awaitingResponseMessages.TryGetValue(message.MessageId, out Tuple<SemaphoreSlim, IpcMessage> awaiting))
                {
                    SemaphoreSlim evt = awaiting.Item1;

                    awaitingResponseMessages.Remove(message.MessageId);
                    awaitingResponseMessages.Add(message.MessageId, new Tuple<SemaphoreSlim, IpcMessage>(evt, message));
                    evt.Release();
                    return true;
                }
            }
            return false;
        }

        protected virtual void OnConnected()
        {
            if (!IsConnected)
            {
                IsConnected = true;
                ConnectedEvent.Set();
                Connected?.Invoke(this, null);
            }
        }

        protected virtual void OnDisconnect(string reason)
        {
            if (IsConnected)
            {
                writeStream?.Dispose();
                readStream?.Dispose();
                IsConnected = false;
                Disconnected?.Invoke(this, reason);
            }
            

        }

        protected virtual void OnError(Exception ex)
        {
            if (!disposedValue)
            {
                OnDisconnect(ex.Message);
            }
                
        }

        /// <summary>
        /// Derived classes should override this method to process serialized messages
        /// </summary>
        /// <param name="message"></param>
        protected virtual void ProcessMessage(IpcMessage message) { }

        /// <summary>
        /// Derived classes should override this method to process non serialized messages
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        protected virtual void ProcessMessage(IpcMessageType type, byte[] data) { }

        /// <summary>
        /// Writes a message to the IPC
        /// </summary>
        /// <param name="message"></param>
        public void Write(IpcMessage message)
        {
            if (disposedValue)
                return;

            if (useSerializer)
                Write(IpcMessageSerializer.Serialize(message));
            else
                Write(message.ToBytes());
        }

        /// <summary>
        /// Writes a raw byte array to the IPC. (automatically includes size header)
        /// </summary>
        /// <param name="data"></param>
        protected void Write(byte[] data)
        {
            if (dedicatedWriteThread)
            {
                writeQueue.Add(data);
            }
            else
            {
                try
                {
                    byte[] p = new byte[data.Length + 4];
                    Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, p, 0, 4);
                    Buffer.BlockCopy(data, 0, p, 4, data.Length);
                    writeStream.Write(p);
                }catch(Exception ex)
                {
                    OnError(ex);
                }
            }
            
        }

        #region IDisposable Support
        protected bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    OnDisconnect("Ipc object disposed");

                    if(dedicatedWriteThread)
                        writeTaskCancel.Cancel();

                    readStream?.Dispose();
                    writeStream.Dispose();
                    readBuffer = null;
                    ConnectedEvent?.Dispose();
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
