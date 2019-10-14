using InputshareLib.Server;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace InputshareLib.Net
{
    internal class ISClientListener
    {
        public bool Listening { get; private set; }
        public IPEndPoint BoundAddress { get; private set; }
        public event EventHandler<ClientConnectedArgs> ClientConnected;

        private TcpListener listener;
        private CancellationTokenSource cancelSource;

        public ISClientListener(int bindPort, IPAddress bindAddress = null)
        {
            if (bindPort == 0)
                throw new ArgumentNullException("bindport");

            if (bindAddress == null)
                bindAddress = IPAddress.Any;

            cancelSource = new CancellationTokenSource();

            listener = new TcpListener(bindAddress, bindPort);
            listener.Start(6);
            listener.BeginAcceptSocket(Listener_AcceptSocketCallback, null);
            BoundAddress = new IPEndPoint(bindAddress, bindPort);
            Listening = true;
        }

        public void Stop()
        {
            if (!Listening)
                throw new InvalidOperationException("ClientListener not listening");

            listener.Stop();
            cancelSource.Cancel();
            Listening = false;
        }

        private void Listener_AcceptSocketCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = listener.EndAcceptSocket(ar);

                if (cancelSource.IsCancellationRequested)
                    return;

                ISLogger.Write("ISClientListener-> Accepting connection from {0}", client.RemoteEndPoint.ToString());
                ISServerSocket clientSoc = new ISServerSocket(client);
                clientSoc.RequestInitialInfo();
                clientSoc.ConnectionError += ClientSocket_ConnectionError;
                clientSoc.InitialInfoReceived += ClientSoc_InitialInfoReceived;
                listener.BeginAcceptSocket(Listener_AcceptSocketCallback, null);
            }
            catch (ObjectDisposedException)
            {
            }catch(Exception ex)
            {
                ISLogger.Write("ISClientListener-> An error occurred while accepting client socket: {0}", ex.Message);
                listener.BeginAcceptSocket(Listener_AcceptSocketCallback, null);
            }
        }

        private void ClientSoc_InitialInfoReceived(object sender, ISServerSocket.InitialInfoReceivedArgs e)
        {
            ISServerSocket client = sender as ISServerSocket;
            client.ConnectionError -= ClientSocket_ConnectionError;
            client.InitialInfoReceived -= ClientSoc_InitialInfoReceived;
            if(e.ClientVer != Settings.InputshareVersion)
            {
                ISLogger.Write("Declining connection from {0}: version mismatch (server is running {1} | client is running {2}", e.Name, Settings.InputshareVersion, e.ClientVer);
                client.DeclineClient(ISServerSocket.ClientDeclinedReason.VersionMismatch, e.ClientVer);
                client.Dispose();
            }
            else
            {
                ClientConnected?.Invoke(this, new ClientConnectedArgs(e.Name, e.Id, e.DisplayConf, client));
            }
        }

        private void ClientSocket_ConnectionError(object sender, string error)
        {
            ISServerSocket client = sender as ISServerSocket;
            ISLogger.Write("ISClientListener-> {0} lost connection: {1}", client.ClientEndpoint, error);
            client.Dispose();
            client.ConnectionError -= ClientSocket_ConnectionError;
        }
        
        public class ClientConnectedArgs
        {
            public ClientConnectedArgs(string clientName, Guid clientId, byte[] displayConfig, ISServerSocket socket)
            {
                DisplayConfig = displayConfig;
                Socket = socket;
                ClientId = clientId;
                ClientName = clientName;
            }

            public byte[] DisplayConfig { get; }
            public ISServerSocket Socket { get; }
            public Guid ClientId { get; }
            public string ClientName { get; }
        }
    }
}
