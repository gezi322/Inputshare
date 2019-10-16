using InputshareLib;
using InputshareLibWindows.IPC.NamedIpc.Messages;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Net;
using System.Text;
using System.Threading;

namespace InputshareLibWindows.IPC.NamedIpc
{
    public sealed class NamedIpcClient : IDisposable
    {
        private NamedPipeClientStream clientStream;
        private byte[] readBuffer = new byte[1024];

        public event EventHandler Disconnected;
        public bool Connected { get; private set; }

        private Dictionary<Guid, Tuple<AutoResetEvent, NamedIpcMessage>> awaitingMessages = new Dictionary<Guid, Tuple<AutoResetEvent, NamedIpcMessage>>();
        private object awaitingMessagesLock = new object();

        public event EventHandler<ServiceConnectionState> StateReceived;

        public NamedIpcClient()
        {
            clientStream = new NamedPipeClientStream(".","inputsharepipe", PipeDirection.InOut, PipeOptions.Asynchronous);
            clientStream.Connect(3000);
            Connected = true;
            ISLogger.Write("NamedIpcClient: Connected");
            clientStream.BeginRead(readBuffer, 0, 4, ReadCallback, null);
            //Write(NamedIpcMessageType.ClientOK);
        }

        public ServiceConnectionState GetState()
        {
            NamedIpcStateResponseMessage message = (NamedIpcStateResponseMessage)SendRequest(new NamedIpcMessage(NamedIpcMessageType.StateRequest));
            ISLogger.Write("GOT STATE RESPONSE");
            IPEndPoint ipe;

            if (message.ConnectedAddress == "")
                ipe = new IPEndPoint(IPAddress.Any, 0);
            else
                ipe = IPEndPoint.Parse(message.ConnectedAddress);

            ISLogger.Write("return state = " + message.Connected);
            return new ServiceConnectionState(message.Connected, ipe, message.Name, message.ClientId, message.AutoReconnect);
        }

        public void Connect(IPEndPoint address, string clientName)
        {
            Write(new NamedIpcConnectMessage(address.Address.ToString(), address.Port, clientName));
        }

        public void Disconnect()
        {
            Write(NamedIpcMessageType.Disconnect);
        }

        private int bytesIn;
        private void ReadCallback(IAsyncResult ar)
        {
            try
            {
                bytesIn = clientStream.EndRead(ar);

                do
                {
                    bytesIn += clientStream.Read(readBuffer, bytesIn, 4 - bytesIn);
                } while (bytesIn < 4);

                int header = BitConverter.ToInt32(readBuffer, 0);

                //TODO - temp
                if (header > readBuffer.Length)
                    readBuffer = new byte[header + 4];
                bytesIn = clientStream.Read(readBuffer, 4, header);
                do
                {
                    bytesIn += clientStream.Read(readBuffer, bytesIn + 4, header - bytesIn);
                } while (bytesIn < header);

                try
                {
                    byte[] data = new byte[bytesIn];
                    Buffer.BlockCopy(readBuffer, 4, data, 0, bytesIn);
                    ISLogger.Write("Received " + data.Length + " bytes");
                    PreProcessMessage(data);
                }
                catch (Exception ex)
                {
                    ISLogger.Write("NamedIpcClient: An error occurred while processing message: " + ex.Message);
                }


                clientStream.BeginRead(readBuffer, 0, 4, ReadCallback, null);
            }
            catch (Exception ex)
            {
                ISLogger.Write("NamedIpcClient: Read error: " + ex.Message);
                Connected = false;
                Disconnected?.Invoke(this, null);
            }
        }

        public void SetAutoReconnect(bool enabled)
        {
            if (enabled)
                Write(NamedIpcMessageType.EnableAutoReconnect);
            else
                Write(NamedIpcMessageType.DisableAutoReconnect);
        }

        private void PreProcessMessage(byte[] data)
        {
            NamedIpcMessage msg = NamedIpcMessageSerializer.DeSerialize(data);

            bool handled = false;
            lock (awaitingMessagesLock)
            {
                Guid id = msg.MessageId;

                if (awaitingMessages.TryGetValue(id, out Tuple<AutoResetEvent, NamedIpcMessage> awaiting))
                {
                    AutoResetEvent evt = awaiting.Item1;

                    awaitingMessages.Remove(id);
                    awaitingMessages.Add(id, new Tuple<AutoResetEvent, NamedIpcMessage>(evt, msg));
                    evt.Set();
                    handled = true;
                }
            }


            if (!handled)
                ProcessMessage(msg);
        }

        private void ProcessMessage(NamedIpcMessage message)
        {
            if(message.MessageType == NamedIpcMessageType.StateResponse)
            {
                NamedIpcStateResponseMessage msg = (message as NamedIpcStateResponseMessage);

                if (!IPEndPoint.TryParse(msg.ConnectedAddress, out IPEndPoint addr))
                    StateReceived?.Invoke(this, new ServiceConnectionState(msg.Connected, new IPEndPoint(IPAddress.Any, 0), msg.Name, msg.ClientId, msg.AutoReconnect));
                else
                    StateReceived?.Invoke(this, new ServiceConnectionState(msg.Connected, addr, msg.Name, msg.ClientId, msg.AutoReconnect));


            }
        }

      

        private NamedIpcMessage SendRequest(NamedIpcMessage message)
        {
            AutoResetEvent evt = new AutoResetEvent(false);
            NamedIpcMessage returnedMessage;
            lock (awaitingMessagesLock)
            {
                awaitingMessages.Add(message.MessageId, new Tuple<AutoResetEvent, NamedIpcMessage>(evt, new NamedIpcMessage(NamedIpcMessageType.ClientOK)));
            }

            Write(message);
            bool cancelled = !evt.WaitOne(5000);

            lock (awaitingMessagesLock)
            {
                evt.Dispose();
                awaitingMessages.TryGetValue(message.MessageId, out Tuple<AutoResetEvent, NamedIpcMessage> ret);
                returnedMessage = ret.Item2;
                awaitingMessages.Remove(message.MessageId);
            }

            if (cancelled)
                throw new TimeoutException();

            return returnedMessage;
        }

        private void Write(NamedIpcMessageType message)
        {
            Write(new NamedIpcMessage(message));
        }

        private void Write(NamedIpcMessage message)
        {
            Write(NamedIpcMessageSerializer.Serialize(message));
        }

        private void Write(byte[] data)
        {
            clientStream.BeginWrite(BitConverter.GetBytes(data.Length), 0, 4, WriteCallback, null);
            clientStream.BeginWrite(data, 0, data.Length, WriteCallback, null);
        }

        private void WriteCallback(IAsyncResult ar)
        {
            try
            {
                clientStream.EndWrite(ar);
            }
            catch (Exception ex)
            {
                ISLogger.Write("NamedIpcClient: Write error: " + ex.Message);
                Connected = false;
                Disconnected?.Invoke(this, null);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    clientStream.Dispose();
                }
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~NamedIpcClient()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

       public class ServiceConnectionState
        {
            public ServiceConnectionState(bool connected, IPEndPoint connectedAddress, string clientName, Guid clientId, bool autoReconnect)
            {
                Connected = connected;
                ConnectedAddress = connectedAddress;
                ClientName = clientName;
                ClientId = clientId;
                AutoReconnect = autoReconnect;
            }

            public bool Connected { get; }
            public IPEndPoint ConnectedAddress { get; }
            public string ClientName { get; }
            public Guid ClientId { get; }
            public bool AutoReconnect { get; }
        }
    }
}
