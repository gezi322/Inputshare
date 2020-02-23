using Inputshare.Common.Net.UDP.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Inputshare.Common.Net.Broadcast
{
    internal sealed class BroadcastSender : IDisposable
    {
        private UdpClient _broadcastSocket;
        private IPEndPoint _broadcastAddress;
        private Timer _broadcastTimer;
        private IPEndPoint _serverAddress;
        private string _version;


        public static BroadcastSender Create(int broadcastInterval, int localServerPort, int broadcastPort, string serverVersion)
        {
            BroadcastSender instance = new BroadcastSender();
            instance._broadcastAddress = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
            instance._serverAddress = GetLocalAddress(localServerPort);
            instance._broadcastSocket = new UdpClient();
            instance._broadcastTimer = new Timer(instance.BroadcastTimerCallback, 0, 0, broadcastInterval);
            instance._version = serverVersion;
            return instance;
        }

        private void BroadcastTimerCallback(object sync)
        {
            byte[] data = new UdpServerBroadcastMessage(_serverAddress.ToString(), _version).ToBytes();
            _broadcastSocket.Send(data, data.Length, _broadcastAddress);
        }

        private static IPEndPoint GetLocalAddress(int serverPort)
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return new IPEndPoint(host.AddressList.Where(i => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault(), serverPort);
        }

        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _broadcastTimer?.Dispose();
                    _broadcastSocket?.Dispose();
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
