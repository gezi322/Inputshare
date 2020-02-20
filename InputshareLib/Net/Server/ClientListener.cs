using InputshareLib.Net.Formatting;
using InputshareLib.Net.Messages;
using InputshareLib.Net.RFS;
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
        internal void BeginListening(IPEndPoint bindAddress, RFSController fileController)
        {
            _listener = new TcpListener(bindAddress);
            _listener.Start();

            _tokenSource = new CancellationTokenSource();
            BindAddress = bindAddress;
            _tokenSource.Token.Register(() => _listener.Stop());
            Listening = true;
            Logger.Write($"Listening at {bindAddress}");
            Task.Run(async() => { await ListenLoop(fileController); });
        }

        internal async Task ListenLoop(RFSController fileController)
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptSocketAsync();
                    await Task.Run(() => ProcessClient(client, fileController));
                }
                catch (ObjectDisposedException) when (_tokenSource.IsCancellationRequested)
                {

                    foreach (var client in _processingClients)
                        client.Dispose();

                    _processingClients.Clear();
                    _listener.Stop();
                    Logger.Write("Stopped listening");
                    Listening = false;
                }
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
        private async Task ProcessClient(Socket client, RFSController fileController)
        {
            CancellationTokenSource cts = null;
            IPEndPoint clientAddr = client.RemoteEndPoint as IPEndPoint;

            try
            {
                _processingClients.Add(client);
                var stream = new NetworkStream(client);
                
                Logger.Write($"Accepting connection from {clientAddr}");

                //Use a cancellationtoken to timeout after 3000ms
                cts = new CancellationTokenSource(3000);


                int bytesIn = 0;
                byte[] clientBuff = new byte[2048];
                
                //Wait for a message header
                while (bytesIn < NetMessageHeader.HeaderSize)
                    bytesIn += await stream.ReadAsync(clientBuff, bytesIn, NetMessageHeader.HeaderSize - bytesIn, cts.Token);

                var header = NetMessageHeader.ReadFromBuffer(clientBuff, 0);

                //Read the full message into the buffer
                bytesIn = 0;
                while (bytesIn < header.MessageLength)
                    bytesIn += await stream.ReadAsync(clientBuff, bytesIn, header.MessageLength - bytesIn, cts.Token);

                //Deserialize message, if incorrect message type is sent then throw
                NetClientConnectionMessage msg = (NetClientConnectionMessage)MessageSerializer.Deserialize(clientBuff, ref header);

                ClientConnected?.Invoke(this, new ClientConnectedArgs(new ServerSocket(client, fileController), msg.ClientName, msg.ClientId, msg.DisplayBounds));
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
