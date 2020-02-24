using Inputshare.Common.Net.UDP.Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inputshare.Common.Net.UDP
{
    internal class ServerUdpSocket : UdpBase
    {
        private Socket _udpSocket;
        private CancellationTokenSource _tokenSource;
        private byte[] _buffer = new byte[1024];

        internal static ServerUdpSocket Create(int bindPort)
        {
            ServerUdpSocket instance = new ServerUdpSocket();
            instance.StartUdpHost(bindPort);
            return instance;
        }

        private void StartUdpHost(int bindPort)
        {
            _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _tokenSource = new CancellationTokenSource();
            _udpSocket.Bind(new IPEndPoint(IPAddress.Any, bindPort));
            Task.Run(() => UdpReadLoop());
            Logger.Debug($"Created UDP host socket on port {bindPort}");
        }

        private void UdpReadLoop()
        {
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            while (!_tokenSource.IsCancellationRequested)
            {
                try
                {
                    var iar = _udpSocket.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref sender, null, null);
                    int bytesIn = _udpSocket.EndReceiveFrom(iar, ref sender);
                    HandleDatagram(_buffer, sender as IPEndPoint);
                }catch(Exception) when (_tokenSource.IsCancellationRequested)
                {
                    Logger.Debug($"UDP host socket closed");
                    return;
                }
                catch(Exception ex)
                {
                    Logger.Error($"Failed to read UDP message: {ex.Message}");
                }
            }
        }

        internal override void SendMessage(IUdpMessage message, IPEndPoint address)
        {
            _udpSocket.SendTo(message.ToBytes(), address);
        }

        protected override void DisposeSocket()
        {
            _tokenSource?.Cancel();
            _udpSocket?.Dispose();
        }

        
    }
}
