using InputshareLib.Input;
using InputshareLib.Net.Messages;
using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib.Net
{
    internal abstract class SocketBase : IDisposable
    {
        internal IPEndPoint Address { get; private set; } = new IPEndPoint(IPAddress.Any, 0);
        internal event EventHandler<InputData> InputReceived;

        private const int MaxMessageSize = 2046 * 1024;
        private const int BufferSize = 2048 * 1024;
        private const int SegmentSize = 2044 * 1024;

        private Socket _client;
        private NetworkStream _stream;
        private readonly byte[] _buffer = new byte[BufferSize];
        private CancellationTokenSource _tokenSource;
        private readonly Dictionary<Guid, SocketRequest> _awaitingMessages = new Dictionary<Guid, SocketRequest>();
        private readonly Dictionary<Guid, SegmentedMessageHandler> _incompleteMessages = new Dictionary<Guid, SegmentedMessageHandler>();
        internal SocketBase()
        {
            
        }

        /// <summary>
        /// Creates a background task to listen and process messages from the specified socket
        /// </summary>
        /// <param name="client"></param>
        protected void BeginReceiveData(Socket client)
        {
            _client = client;
            _client.NoDelay = true;
            _tokenSource = new CancellationTokenSource();
            _stream = new NetworkStream(client);
            _awaitingMessages.Clear();
            Address = client.RemoteEndPoint as IPEndPoint;

            //Start receiving data from the client in a new task
            Task.Run(() => ReceiveData());
        }

        private void ReceiveData()
        {
            try
            {
                int bytesIn = 0;

                while (!_tokenSource.IsCancellationRequested)
                {
                    bytesIn = 0;

                    //Read the message header
                    while (bytesIn < NetMessageHeader.HeaderSize)
                        bytesIn += _stream.Read(_buffer, bytesIn, NetMessageHeader.HeaderSize - bytesIn);

                    //Get the message size
                    var header = new NetMessageHeader(_buffer, 0);

                    //Check if header is input data
                    if(header.IsInput)
                    {
                        InputReceived?.Invoke(this, header.Input);
                        continue;
                    }

                    //Read the full message
                    bytesIn = 0;
                    while (bytesIn < header.MessageLength)
                        bytesIn += _stream.Read(_buffer, bytesIn, header.MessageLength - bytesIn);

                    NetMessageBase message = NetMessageSerializer.Deserialize<NetMessageBase>(_buffer);

                    if (message is NetMessageSegment segMsg)
                        HandleMessageSegment(segMsg);
                    else if (message is NetReplyBase replyMessage)
                        HandleReply(replyMessage);
                    else if (message is NetRequestBase requestMessage)
                        HandleRequestAsync(requestMessage);
                    else
                        HandleGenericMessage(message);
                }
            }catch(Exception ex)
            {
                HandleExceptionInternal(ex);
            }
        }

        /// <summary>
        /// Handles a received message segment
        /// </summary>
        /// <param name="message"></param>
        private void HandleMessageSegment(NetMessageSegment message)
        {
            if (!_incompleteMessages.ContainsKey(message.MessageId))
            {
                SegmentedMessageHandler handler = new SegmentedMessageHandler(message.FullPacketSize);

                handler.MessageComplete += (object o, NetMessageBase completeMessage) =>
                {
                    HandleGenericMessage(completeMessage);
                };

                _incompleteMessages.Add(message.MessageId, handler);
                handler.Write(message);
            }
            else
            {
                _incompleteMessages.TryGetValue(message.MessageId, out var handler);
                handler.Write(message);
            }
        }

        private void HandleReply(NetReplyBase replyMessage)
        {
            if (_awaitingMessages.TryGetValue(replyMessage.MessageId, out var ret))
                ret.SetReplyMessage(replyMessage);
        }

        /// <summary>
        /// Sends a request and waits for a reply
        /// </summary>
        /// <typeparam name="TReply">Expected reply</typeparam>
        /// <param name="request">The request message</param>
        /// <returns></returns>
        protected async Task<TReply> SendRequestAsync<TReply>(NetRequestBase request) where TReply : NetReplyBase
        {
            //Create a request object 
            SocketRequest req = new SocketRequest(request);
            //add the request to the awaiting requests dictionary
            _awaitingMessages.Add(request.MessageId, req);
            //Send the request message
            await SendMessageAsync(req.RequestMessage);
            //Wait for a reply message
            var reply = await req.AwaitReply();
            //Remove the request 
            _awaitingMessages.Remove(request.MessageId);
            //Cast the reply to the expected message reply type
            return reply as TReply;
        }

        /// <summary>
        /// Sends a large message in smaller segments
        /// </summary>
        /// <param name="serializedMessage"></param>
        /// <returns></returns>
        private async Task SendMessageSegmentedAsync(byte[] serializedMessage)
        {
            try
            {
                int bOut = 0;
                Guid messageId = Guid.NewGuid();

                while(bOut < serializedMessage.Length)
                {
                    int pSize = serializedMessage.Length - bOut - SegmentSize > 0 ? SegmentSize : serializedMessage.Length - bOut;
                    byte[] data = new byte[pSize];
                    Buffer.BlockCopy(serializedMessage, bOut, data, 0, pSize);
                    await SendMessageAsync(new NetMessageSegment(messageId, data, serializedMessage.Length));
                    bOut += pSize;
                }
            }catch(Exception ex)
            {
                HandleExceptionInternal(ex);
            }
        }

        internal virtual void DisconnectSocket()
        {
            _client?.Dispose();
            _stream?.Dispose();
            _tokenSource?.Dispose();
        }

        /// <summary>
        /// Sends input data to the client
        /// </summary>
        /// <param name="input"></param>
        internal void SendInput(ref InputData input)
        {
            try
            {
                NetMessageHeader header = new NetMessageHeader(input);
                _stream.Write(header.Data, 0, header.Data.Length);
            }
            catch (Exception ex)
            {
                HandleExceptionInternal(ex);
            }
        }

        /// <summary>
        /// Sends a network message to the client asynchronously
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected async Task SendMessageAsync(NetMessageBase message)
        {
            try
            {
                byte[] data = NetMessageSerializer.Serialize(message);
                
                //If the message is too large, send it as smaller segments
                if(data.Length > MaxMessageSize)
                {
                    data = NetMessageSerializer.SerializeNoHeader(message);
                    await SendMessageSegmentedAsync(data);
                    return;
                }

                await _stream.WriteAsync(data, 0, data.Length);
            }catch(Exception ex)
            {
                HandleExceptionInternal(ex);
            }
        }

        /// <summary>
        /// Sends a network message to the client asynchronously
        /// </summary>
        /// <param name="message"></param>
        protected void SendMessage(NetMessageBase message)
        {
            try
            {
                byte[] data = NetMessageSerializer.Serialize(message);
                _stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                HandleExceptionInternal(ex);
            }
        }

        /// <summary>
        /// Handles incoming messages
        /// </summary>
        /// <param name="message"></param>
        protected abstract void HandleGenericMessage(NetMessageBase message);

        /// <summary>
        /// Handles incoming request messages
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract Task HandleRequestAsync(NetRequestBase request);

        private void HandleExceptionInternal(Exception ex)
        {
            HandleException(ex);
        }

        /// <summary>
        /// Handler for an exception that could occur when reading/writing to the network stream
        /// </summary>
        /// <param name="ex"></param>
        protected abstract void HandleException(Exception ex);

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _stream?.Dispose();
                    _tokenSource?.Dispose();
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
