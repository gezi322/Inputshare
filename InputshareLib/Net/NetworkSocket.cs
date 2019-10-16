using InputshareLib.Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib.Net
{

    /// <summary>
    /// Base class for sending and receiving messages
    /// </summary>
    internal abstract class NetworkSocket : IDisposable
    {
        /// <summary>
        /// Occurs when a client successfully drops the current dragdrop operation files to a droptarget
        /// </summary>
        public event EventHandler<Guid> DragDropSuccess;

        /// <summary>
        /// Occurs when the client or server cancels the current drag drop operation
        /// </summary>
        public event EventHandler<Guid> DragDropCancelled;

        /// <summary>
        /// Occurs when an urecoverable connection error occurs
        /// </summary>
        public event EventHandler<string> ConnectionError;
        
        /// <summary>
        /// Occurs when the client or server receives a clipboard data message
        /// </summary>
        public event EventHandler<ClipboardDataReceivedArgs> ClipboardDataReceived;

        /// <summary>
        /// Occurs when a client or server receives a dragdrop operation
        /// </summary>
        public event EventHandler<DragDropDataReceivedArgs> DragDropDataReceived;

        /// <summary>
        /// Occurs when the server sends input data
        /// </summary>
        public event EventHandler<byte[]> InputDataReceived;

        /// <summary>
        /// True if the socket is currently connected
        /// </summary>
        public bool IsConnected { get; protected set; } = false;

        /// <summary>
        /// Socket object used for reading/writing
        /// </summary>
        protected Socket tcpSocket;

        /// <summary>
        /// Buffer used by the socket to read data into
        /// </summary>
        private byte[] socketBuff = new byte[Settings.SocketBufferSize];

        /// <summary>
        /// Set when the first exception is caught, to prevent multiple 
        /// callback threads calling connection error/connection failed events
        /// </summary>
        protected bool errorHandled = false;

        /// <summary>
        /// A list of message handlers that are used to store a transfer operation
        /// that would be too large to fit in the socket buffer
        /// </summary>
        private List<LargeMessageHandler> messageHandlers = new List<LargeMessageHandler>();

        public event EventHandler<Guid> DragDropOperationComplete;

        public event EventHandler<FileTokenRequestArgs> RequestedFileToken;
        public event EventHandler<RequestStreamReadArgs> RequestedStreamRead;
        public event EventHandler<RequestCloseStreamArgs> RequestedCloseStream;
        

        //Used to wait for a response to a request
        private object awaitingCollectionsLock = new object();
        private Dictionary<Guid, NetworkMessage> awaitingReturnMessages = new Dictionary<Guid,  NetworkMessage>();
        private Dictionary<Guid, AutoResetEvent> awaitingReturnMethods = new Dictionary<Guid, AutoResetEvent>();
        private List<Guid> awaitingMessageIds = new List<Guid>();

        /// <summary>
        /// Creates a new inputshare network socket from an existing TCP socket
        /// </summary>
        /// <param name="initSocket"></param>
        public NetworkSocket(Socket initSocket)
        {
            tcpSocket = initSocket;
            tcpSocket.LingerState = new LingerOption(true, 1);
            tcpSocket.NoDelay = true; //Disable nagles algorithm!
            IsConnected = true;
            tcpSocket.BeginReceive(socketBuff, 0, 4, SocketFlags.None, TcpSocket_ReadCallback, null);
        }

        /// <summary>
        /// Creates a dummy socket that is used to treat localhost
        /// like any other client
        /// </summary>
        /// <param name="DUMMY"></param>
        public NetworkSocket(bool DUMMY)
        {

        }

        public NetworkSocket()
        {
            
        }

        /// <summary>
        /// Responds to the hosts RequestFileToken with the specified token
        /// </summary>
        /// <param name="messageId">The MessageId of the received RequestFileToken message</param>
        /// <param name="token">The token that will be sent to the host</param>
        public void SendTokenRequestReponse(Guid messageId, Guid token)
        {
            SendMessage(new RequestGroupTokenResponseMessage(token, messageId));
        }

        /// <summary>
        /// Requests a file access token from the host.
        /// The file access token allows this client to access any files within the
        /// current dragdrop or clipboard operation. The token returned from this method
        /// should be used when requesting to read from the remote filestream along with the GUID
        /// of the file that you want to read.
        /// </summary>
        /// <exception cref="RemoteFileStreamReadFailedException"></exception>
        /// <exception cref="InvalidNetworkResponseException"></exception>"
        /// <exception cref="RequestTimedOutException"></exception>
        /// <param name="fileGroupId">The operation ID of the files to access</param>
        /// <returns></returns>
        public async Task<Guid> RequestFileTokenAsync(Guid fileGroupId)
        {
            ISLogger.Write("Sending token request");
                NetworkMessage response = await SendRequestAsync(new RequestGroupTokenMessage(fileGroupId));
                ISLogger.Write("Server responded to token request!");

                switch (response.Type)
                {
                    case MessageType.RequestFileGroupTokenReponse:
                        RequestGroupTokenResponseMessage resp = response as RequestGroupTokenResponseMessage;
                        return resp.Token;
                case MessageType.RemoteFileError:
                    FileErrorMessage err = response as FileErrorMessage;
                    ISLogger.Write("File token request returned error: " + err.ErrorMessage);
                    return Guid.Empty;
                    break;

                    default:
                        ISLogger.Write("Debug: Server sent unexpected reply when requesting file access token");
                        return Guid.Empty;
                }
        }

        /// <summary>
        /// Reponds to a filestream read request and tells the host that an error occurred with
        /// the specified message
        /// </summary>
        /// <param name="networkMessageId">The MessageId used in the read request</param>
        /// <param name="errorMessage">The error message string</param>
        public void SendFileErrorResponse(Guid networkMessageId, string errorMessage)
        {
            SendMessage(new FileErrorMessage(errorMessage, networkMessageId));
        }
        
        /// <summary>
        /// Responds to a filestream read request.
        /// </summary>
        /// <param name="networkMessageId">The MessageId used in the read request</param>
        /// <param name="readData">The data read from the stream</param>
        public void SendReadRequestResponse(Guid networkMessageId, byte[] readData)
        {
            SendMessage(new FileStreamReadResponseMessage(readData, networkMessageId));
        }


        /// <summary>
        /// Sends a request to read a remote filestream from the host and
        /// waits for a response message.
        /// </summary>
        ///<exception cref="RemoteFileStreamReadFailedException"></exception>
        /// <exception cref="InvalidNetworkResponseException"></exception>"
        /// <exception cref="RequestTimedOutException"></exception>
        /// <param name="token">The access token used to access the filestream</param>
        /// <param name="file">The Guid associated with the file</param>
        /// <param name="readLen">Ammount of bytes to read</param>
        /// <returns></returns>
        public async Task<byte[]> RequestReadStreamAsync(Guid token, Guid file, int readLen)
        {
            NetworkMessage response = await SendRequestAsync(new FileStreamReadRequestMessage(token, file, readLen));

            switch (response.Type) {
                case MessageType.FileStreamReadResponse:
                    FileStreamReadResponseMessage resp = response as FileStreamReadResponseMessage;
                    return resp.ReadData;
                case MessageType.RemoteFileError:
                    FileErrorMessage errMsg = response as FileErrorMessage;
                    throw new RemoteFileStreamReadFailedException(errMsg.ErrorMessage);
                default:
                    throw new InvalidNetworkResponseException("Server responded with unexpected message type " + response.Type);
            }
        }

        /// <summary>
        /// Tell the host that a remote filestream should be closed
        /// </summary>
        /// <param name="token"></param>
        /// <param name="file"></param>
        public void RequestCloseStream(Guid token, Guid file)
        {
            SendMessage(new FileStreamCloseStreamMessage(token, file));
        }

        /// <summary>
        /// Sends a request to the server and waits for a reply.
        /// Throws a RequestTimedOutException if no reply is received after 10 seconds
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="RequestTimedOutException"></exception>
        /// <returns></returns>
        private async Task<NetworkMessage> SendRequestAsync(NetworkMessage message)
        {
            try
            {
                return await Task.Run(() =>
                {
                    AutoResetEvent evt = new AutoResetEvent(false);
                    NetworkMessage msg;
                    lock (awaitingCollectionsLock)
                    {
                        awaitingReturnMethods.Add(message.MessageId, evt);
                        awaitingMessageIds.Add(message.MessageId);
                    }
                    
                    SendMessage(message);
                    bool cancelled = !evt.WaitOne(10000);

                    lock (awaitingCollectionsLock)
                    {
                        evt.Dispose();

                        awaitingReturnMessages.TryGetValue(message.MessageId, out msg);
                        awaitingReturnMethods.Remove(message.MessageId);
                        awaitingReturnMessages.Remove(message.MessageId);
                        awaitingMessageIds.Remove(message.MessageId);
                    }

                    if(cancelled)
                        throw new RequestTimedOutException();

                    return msg;
                });
            }
            catch
            {
                throw;
            }
            
        }

        /// <summary>
        /// Notifies the host that either the dragdrop files were dropped, or the operation was cancelled
        /// </summary>
        /// <param name="successful"></param>
        public void NotifyDragDropSuccess(Guid operationId, bool successful)
        {
            if (successful)
                SendMessage(new DragDropSuccessMessage(operationId));
            else
                SendMessage(new DragDropCancelledMessage(operationId));
        }

        /// <summary>
        /// Returns the large message handler associated with the specified GUID
        /// </summary>
        /// <param name="transferId"></param>
        /// <returns></returns>
        protected LargeMessageHandler GetMessageHandlerFromId(Guid transferId)
        {
            return messageHandlers.Where(x => x.MessageId == transferId).FirstOrDefault();
        }

        /// <summary>
        /// Callback from an async thread when a connection attempt fails or succeeds
        /// </summary>
        /// <param name="ar"></param>
        protected void TcpSocket_ConnectCallback(IAsyncResult ar)
        {
            try {
                tcpSocket.EndConnect(ar);

                if (socketBuff == null)
                    socketBuff = new byte[Settings.SocketBufferSize];

                tcpSocket.BeginReceive(socketBuff, 0, 4, 0, TcpSocket_ReadCallback, null);
                OnConnected();
            }
            catch (ObjectDisposedException)
            {

            }catch(SocketException ex)
            {
                HandleConnectedFailed(ex.Message);
            }
        }

        /// <summary>
        /// Closes the socket
        /// </summary>
        public virtual void Close()
        {
            try
            {
                if (IsConnected)
                {
                    tcpSocket.Shutdown(SocketShutdown.Both);
                } 
            }
            finally
            {
                tcpSocket.Dispose();
                IsConnected = false;
            }
        }

        /// <summary>
        /// Sends any message defined in InputshareLib.Net.Messages
        /// Larger messages are automatically split into smalller messages
        /// that can be read by the receiving socket
        /// </summary>
        /// <param name="message"></param>
        protected void SendMessage(NetworkMessage message)
        {
            try
            {
                byte[] data = message.ToBytes();
                if (data.Length > Settings.NetworkMessageChunkSize)
                {
                    SendLargeMessage(message);
                    return;
                }

                if (tcpSocket != null && tcpSocket.Connected)
                    tcpSocket.BeginSend(data, 0, data.Length, SocketFlags.None, TcpSocket_SendCallback, null);
            }catch(Exception ex)
            {
                ISLogger.Write("Sendmessage error: " + ex.Message);
            }
            
        }

        /// <summary>
        /// Sends a file that is larger than the size specified in InputshareLib.Settings.NetworkMessageChunkSize
        /// </summary>
        /// <param name="message"></param>
        private void SendLargeMessage(NetworkMessage message)
        {
            Task.Run(() => {
                byte[] data = message.ToBytes();
                Guid transferId = Guid.NewGuid();

                int hPos = 0;
                int hRem = data.Length;
                do
                {
                    int partLen = Settings.NetworkMessageChunkSizeNoHeader;
                    if (hRem - partLen < 0)
                    {
                        partLen = hRem;
                    }

                    byte[] part = new byte[partLen];
                    Buffer.BlockCopy(data, hPos, part, 0, partLen);
                    MessageChunkMessage chunk = new MessageChunkMessage(transferId, part, data.Length);
                    SendMessage(chunk);

                    hPos += partLen;
                    hRem -= partLen;
                } while (hRem > 0);
            });
        }

        
        /// <summary>
        /// Callback from an async thread when a send operation succeeds or fails
        /// </summary>
        /// <param name="ar"></param>
        protected void TcpSocket_SendCallback(IAsyncResult ar)
        {
            try
            {
                int bytesOut = tcpSocket.EndSend(ar);
            }
            catch (ObjectDisposedException)
            {

            }
            catch (SocketException ex)
            {
                ISLogger.Write("Error sending data : " + ex.Message);
            }
        }

        /// <summary>
        /// Callback from an async thread when a read operation succeeds or fails
        /// </summary>
        /// <param name="ar"></param>
        private void TcpSocket_ReadCallback(IAsyncResult ar)
        {
            try
            {
                int bytesIn = tcpSocket.EndReceive(ar);

                //If we receive 0 bytes, the connection is closed
                if(bytesIn == 0)
                {
                    HandleConnectionClosed("Client closed connection");
                    return;
                }

                int dRem;
                int hPos;

                //Make sure that all 4 bytes are recieved
                if (bytesIn != 4)
                {
                    hPos = bytesIn;
                    dRem = 4 - bytesIn;
                    do
                    {
                        int hIn = tcpSocket.Receive(socketBuff, hPos, dRem, 0);
                        hPos += hIn;
                        dRem -= hIn;
                    } while (dRem > 0);

                }

                //Read the size of the packet from the header (-4 as header is already read)
                int packetBodySize = BitConverter.ToInt32(socketBuff, 0)-4;
                //Make sure the packet will fit in the socket buffer
                if(packetBodySize > socketBuff.Length-4 || 0 > packetBodySize)
                {
                    HandleConnectionClosed("Client sent packet larger than buffer (" + packetBodySize +")");
                    return;
                }

                //Wait for all bytes of the packet to be read
                dRem = packetBodySize;
                hPos = 4;
                do
                {
                    int bIn = tcpSocket.Receive(socketBuff, hPos, dRem, 0);
                    hPos += bIn;
                    dRem = packetBodySize - hPos + 4;
                } while (dRem > 0);

                HandleReceivedMessage(packetBodySize + 4);
                tcpSocket.BeginReceive(socketBuff, 0, 4, 0, TcpSocket_ReadCallback, null);
            }
            catch (ObjectDisposedException)
            {
                return;
            }catch (SocketException ex)
            {
                HandleConnectionClosed(ex.Message);
            }
        }

        /// <summary>
        /// Sends a drag drop operation to the client/server
        /// </summary>
        /// <param name="data"></param>
        public void SendDragDropData(byte[] data, Guid operationId)
        {
            SendMessage(new DragDropDataMessage(data, operationId));
        }

        /// <summary>
        /// Tells the host to start a dragdrop operation with speciifed files and operation ID
        /// </summary>
        /// <param name="data"></param>
        public virtual void SendClipboardData(byte[] data, Guid operationId)
        {
            SendMessage(new ClipboardDataMessage(data, operationId));
        }

        public void SendDragDropComplete(Guid operationId)
        {
            SendMessage(new DragDropCompleteMessage(operationId));
        }

        /// <summary>
        /// Handles incoming messages and checks for message chunks,
        /// which are then read into the appropriate LargeMessageHandler
        /// </summary>
        /// <param name="messageSize"></param>
        private void HandleReceivedMessage(int messageSize, byte[] data = null)
        {
            byte[] source = null;

            if (data is null)
                source = socketBuff;
            else
                source = data;

            MessageType type = (MessageType)source[4];
            
            if(type == MessageType.InputData)
            {
                byte[] input = new byte[5];
                Buffer.BlockCopy(source, 5, input, 0, 5);
                InputDataReceived?.Invoke(this, input);
                return;
            }
            else if(type == MessageType.MessagePart)
            {
                HandleMessageChunkReceived(new MessageChunkMessage(socketBuff));
                return;
            }

            HandleMessage(source);
        }
        /// <summary>
        /// Called when a dragdrop message is received
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleDragDropMessage(DragDropDataMessage message)
        {
            DragDropDataReceived?.Invoke(this, new DragDropDataReceivedArgs(message.cbData, message.OperationId));
        }


        List<Guid> ProcessedMessageIds = new List<Guid>();
        /// <summary>
        /// Handles messages received from the client/server. Derived
        /// classes must override this method to process messages. Derived 
        /// classes should call the base method after processing the message.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleMessage(byte[] message)
        {

            MessageType type = (MessageType)message[4];
            NetworkMessage msg = new NetworkMessage(message);

            //Make sure we don't somehow handle the same request more than once
            if (ProcessedMessageIds.Contains(msg.MessageId))
            {
                ISLogger.Write("Ignoring duplicate message ID");
                return;
            }

            if (type == MessageType.ClipboardData)
            {
                HandleClipboardData(new ClipboardDataMessage(message));
            }
            else if (type == MessageType.DragDropData)
            {
                HandleDragDropMessage(new DragDropDataMessage(message));
            }
            else if (type == MessageType.DragDropSuccess)
            {
                DragDropSuccessMessage sMsg = new DragDropSuccessMessage(message);
                DragDropSuccess?.Invoke(this, sMsg.OperationId);
            }
            else if (type == MessageType.DragDropCancelled)
            {
                DragDropCancelledMessage cMsg = new DragDropCancelledMessage(message);
                DragDropCancelled?.Invoke(this, cMsg.OperationId);
            }
            else if (type == MessageType.RequestFileGroupToken)
            {
                RequestGroupTokenMessage requestMsg = new RequestGroupTokenMessage(message);
                RequestedFileToken?.Invoke(this, new FileTokenRequestArgs(requestMsg.MessageId, requestMsg.FileGroupId));
            }
            else if (type == MessageType.FileStreamReadRequest)
            {
                FileStreamReadRequestMessage requestMsg = new FileStreamReadRequestMessage(message);
                RequestedStreamRead?.Invoke(this, new RequestStreamReadArgs(requestMsg.MessageId, requestMsg.Token, requestMsg.FileRequestId, requestMsg.ReadSize));
            }else if(type == MessageType.FileStreamCloseRequest)
            {
                FileStreamCloseStreamMessage closeMsg = new FileStreamCloseStreamMessage(message);
                RequestedCloseStream?.Invoke(this, new RequestCloseStreamArgs(closeMsg.Token, closeMsg.FileId));
            }else if(type == MessageType.DragDropComplete) {
                DragDropCompleteMessage ddcMsg = new DragDropCompleteMessage(message);
                DragDropOperationComplete?.Invoke(this, ddcMsg.OperationId);
            }

            lock (awaitingCollectionsLock)
            {
                if (awaitingMessageIds.Contains(msg.MessageId))
                {
                    //TODO
                    if (type == MessageType.RequestFileGroupTokenReponse)
                    {
                        awaitingReturnMessages.Add(msg.MessageId, new RequestGroupTokenResponseMessage(message));
                    }
                    else if (type == MessageType.FileStreamReadResponse)
                    {
                        awaitingReturnMessages.Add(msg.MessageId, new FileStreamReadResponseMessage(message));
                    }
                    else if (type == MessageType.RemoteFileError)
                    {
                        awaitingReturnMessages.Add(msg.MessageId, new FileErrorMessage(message));
                    }

                    awaitingReturnMethods.TryGetValue(msg.MessageId, out AutoResetEvent evt);
                    evt.Set();
                }
            }
            

        }
        
        public void RespondReadStream(Guid networkMessageId, byte[] readData)
        {
            SendMessage(new FileStreamReadResponseMessage(readData, networkMessageId));
        }

        /// <summary>
        /// Called when the socket receives a message chunk, and stores
        /// it in the appropriate LargeMessageHandler based on the message GUID
        /// </summary>
        /// <param name="message"></param>
        private void HandleMessageChunkReceived(MessageChunkMessage message)
        {
            LargeMessageHandler handler = GetMessageHandlerFromId(message.MessageId);

            if(handler == null)
            {
                //ISLogger.Write("Incoming large packet (" + message.MessageSize / 1024 + "KB)");
                handler = new LargeMessageHandler(message.MessageId, message.MessageSize);
                messageHandlers.Add(handler);
            }

            handler.Write(message.MessageData);
           
            if (handler.FullyReceived)
            {
                byte[] data = handler.ReadAndClose();
                messageHandlers.Remove(handler);
                handler = null;
                HandleReceivedMessage(message.MessageSize, data);
            }
        }
        
        /// <summary>
        /// Sends a raw byte array to the server/client. The byte array
        /// must start with an integer as the first 4 bytes that are set to the
        /// total size of the byte array including the first 4 bytes.
        /// </summary>
        /// <param name="data"></param>
        protected void SendRawData(byte[] data)
        {
            try
            {
                tcpSocket.BeginSend(data, 0, data.Length, 0, TcpSocket_SendCallback, null);
            }catch(SocketException ex)
            {
                HandleConnectionClosed(ex.Message);
            }
            
        }

        /// <summary>
        /// Occurs when clipboard data is received. Fires the 
        /// ClipboardDataReceived event
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleClipboardData(ClipboardDataMessage message)
        {
            ISLogger.Write(message.OperationId);
            ClipboardDataReceived?.Invoke(this, new ClipboardDataReceivedArgs(message.cbData, message.OperationId));
        }

        /// <summary>
        /// Cleans up after a connection error or a call to Close()
        /// </summary>
        /// <param name="error"></param>
        protected virtual void HandleConnectionClosed(string error) {
            IsConnected = false;

            if (!errorHandled)
                errorHandled = true;
            else
                return;

            try
            {
                tcpSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception) { }
            
            ConnectionError?.Invoke(this, error);
        }

        protected virtual void HandleConnectedFailed(string error)
        {

        }

        protected virtual void OnConnected()
        {

        }

        public enum CloseNotifyMode
        {
            None = 0,
            ServerStopped = 1
        }

        #region eventargs and exceptions
        public class RemoteFileStreamReadFailedException : Exception
        {
            public RemoteFileStreamReadFailedException(string message) : base(message)
            {
   
            }
        }

        public class InvalidNetworkResponseException : Exception
        {
            public InvalidNetworkResponseException(string message) : base(message)
            {

            }
        }

        public class RequestCloseStreamArgs : EventArgs
        {
            public RequestCloseStreamArgs(Guid token, Guid file)
            {
                Token = token;
                File = file;
            }

            public Guid Token { get; }
            public Guid File { get; }
        }

        public class FileTokenRequestArgs : EventArgs
        {
            public FileTokenRequestArgs(Guid networkMessageId, Guid fileGroupId)
            {
                NetworkMessageId = networkMessageId;
                FileGroupId = fileGroupId;
            }
            public Guid NetworkMessageId { get; }
            public Guid FileGroupId { get; }
        }
        public class ClipboardDataReceivedArgs : EventArgs
        {
            public ClipboardDataReceivedArgs(byte[] rawData, Guid operationId)
            {
                this.rawData = rawData;
                OperationId = operationId;
            }

            public byte[] rawData { get; }
            public Guid OperationId { get; }
        }

        public class RequestStreamReadArgs : EventArgs
        {
            public RequestStreamReadArgs(Guid networkMessageId, Guid token, Guid file, int readLen)
            {
                NetworkMessageId = networkMessageId;
                Token = token;
                File = file;
                ReadLen = readLen;
            }

            public Guid NetworkMessageId { get; }
            public Guid Token { get; }
            public Guid File { get; }
            public int ReadLen { get; }
        }

        
        public class DragDropDataReceivedArgs : EventArgs
        {
            public DragDropDataReceivedArgs(byte[] rawData, Guid operationId)
            {
                RawData = rawData;
                OperationId = operationId;
            }

            public byte[] RawData { get; }
            public Guid OperationId { get; }
        }
        #endregion


        #region IDisposable Support
        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach(var handle in messageHandlers)
                    {
                        handle.Close();
                    }

                    tcpSocket.Dispose();
                    socketBuff = null;
                }

                disposedValue = true;
            }
        }

        public class RequestTimedOutException : Exception
        {
            public RequestTimedOutException() : base()
            {

            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
