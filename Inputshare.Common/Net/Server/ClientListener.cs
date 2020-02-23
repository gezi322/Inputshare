using Inputshare.Common.Net.Formatting;
using Inputshare.Common.Net.Messages;
using Inputshare.Common.Net.RFS;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inputshare.Common.Net.Server
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
            _listener.Start(6);

            _tokenSource = new CancellationTokenSource();
            BindAddress = bindAddress;
            _tokenSource.Token.Register(() => _listener.Stop());
            Listening = true;
            Logger.Debug($"Listening at {bindAddress}");
            Task.Run(async() => { await ListenLoop(fileController); });
        }

        internal async Task ListenLoop(RFSController fileController)
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptSocketAsync();
                    Logger.Debug($"Accepted socket {client.RemoteEndPoint}");
                    await ProcessClient(client, fileController);
                }
                catch (ObjectDisposedException) when (_tokenSource.IsCancellationRequested)
                {

                    foreach (var client in _processingClients)
                        client.Dispose();

                    _processingClients.Clear();
                    _listener.Stop();
                    Logger.Verbose("Stopped listening");
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
                
                Logger.Verbose($"Accepting connection from {clientAddr}");

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
                Logger.Verbose($"Valid data received from {clientAddr}: {msg.ClientName} {msg.ClientVersion} {msg.DisplayBounds} {msg.UdpPort}");
                ClientConnected?.Invoke(this, new ClientConnectedArgs(new ServerSocket(client, fileController), msg.ClientName, msg.ClientId, msg.DisplayBounds, msg.UdpPort));
            }catch(OperationCanceledException) when (cts.IsCancellationRequested)
            {
                Logger.Error($"{clientAddr} timed out");
                client?.Dispose();
            }catch(Exception ex)
            {
                Logger.Error($"Failed to accept connection from {clientAddr}: {ex.Message}\n{ex.StackTrace}");
                client?.Dispose();
            }
            finally
            {
                _processingClients.Remove(client);
            }
        }


    }
}
