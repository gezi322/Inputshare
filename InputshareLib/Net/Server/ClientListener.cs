using InputshareLib.Net.Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib.Net.Server
{
    internal sealed class ClientListener
    {
        internal event EventHandler<ClientConnectedArgs> ClientConnected;

        internal bool Listening { get; private set; }
        internal IPEndPoint BindAddress { get; private set; }

        private TcpListener _listener;
        private CancellationTokenSource _tokenSource;
        private readonly List<Socket> _processingClients = new List<Socket>();

        /// <summary>
        /// listens for connections on the specified address
        /// </summary>
        /// <param name="bindAddress"></param>
        /// <returns></returns>
        internal async Task ListenAsync(IPEndPoint bindAddress)
        {
            _listener = new TcpListener(bindAddress);
            _listener.Start();
            _tokenSource = new CancellationTokenSource();
            BindAddress = bindAddress;
            _tokenSource.Token.Register(() => _listener.Stop());
            Listening = true;

            Logger.Write($"Listening at {bindAddress}");

            try
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    var client = await _listener.AcceptSocketAsync();
                    Task.Run(() => ProcessClient(client));
                }
            }catch(ObjectDisposedException) when (_tokenSource.IsCancellationRequested)
            {
                //If Stop() is called
            }
            finally
            {
                foreach (var client in _processingClients)
                    client.Dispose();

                _processingClients.Clear();
                _listener.Stop();
                Logger.Write("Stopped listening");
                Listening = false;
            }
        }

        internal void Stop()
        {
            if(Listening)
                _tokenSource.Cancel();
        }

        /// <summary>
        /// Accepts an incoming connection and waits for it to send a valid clientconnection message
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private async Task ProcessClient(Socket client)
        {
            _processingClients.Add(client);
            var stream = new NetworkStream(client);
            IPEndPoint clientAddr = client.RemoteEndPoint as IPEndPoint;
            Logger.Write($"Accepting connection from {clientAddr}");

            //Use a cancellationtoken to timeout after 3000ms
            using CancellationTokenSource cts = new CancellationTokenSource(3000);

            try
            {
                int bytesIn = 0;
                byte[] clientBuff = new byte[2048];
                
                //Wait for a message header
                while (bytesIn < NetMessageHeader.HeaderSize)
                    bytesIn += await stream.ReadAsync(clientBuff, bytesIn, NetMessageHeader.HeaderSize - bytesIn, cts.Token);

                var header = new NetMessageHeader(clientBuff, 0);

                //Read the full message into the buffer
                bytesIn = 0;
                while (bytesIn < header.MessageLength)
                    bytesIn += await stream.ReadAsync(clientBuff, bytesIn, header.MessageLength - bytesIn, cts.Token);

                //Deserialize message, if incorrect message type is sent then throw
                NetClientConnectionMessage msg = NetMessageSerializer.Deserialize<NetClientConnectionMessage>(clientBuff);

                ClientConnected?.Invoke(this, new ClientConnectedArgs(new ServerSocket(client), msg.ClientName, msg.ClientId, msg.DisplayBounds));
            }catch(OperationCanceledException) when (cts.IsCancellationRequested)
            {
                Logger.Write($"{clientAddr} timed out");
                client?.Dispose();
            }catch(Exception ex)
            {
                Logger.Write($"Failed to accept connection from {client.RemoteEndPoint}: {ex.Message}");
                client?.Dispose();
            }
            finally
            {
                _processingClients.Remove(client);
            }
        }


    }
}
