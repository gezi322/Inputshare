using System;
using System.Net;
using System.Net.Sockets;

namespace InputshareLib.Net.Udp
{
    internal class ISUdpClient : IDisposable
    {
        public event EventHandler<byte[]> InputReceived;

        /// <summary>
        /// True if a response has been received from the server
        /// </summary>
        public bool Connected { get; private set; }

        private Socket udpSocket;
        private byte[] readBuff = new byte[4096];

        public IPEndPoint UdpBindAddress { get; private set; } = new IPEndPoint(IPAddress.Any, 0);
        private IPEndPoint serverUdpEndPoint;
        private EndPoint _ep = new IPEndPoint(IPAddress.Any, 0);
        private IPEndPoint receivedFromAddress { get => _ep as IPEndPoint; }

        internal ISUdpClient(IPEndPoint serverUdpAddress)
        {
            serverUdpEndPoint = serverUdpAddress;
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(UdpBindAddress);
            UdpBindAddress = udpSocket.LocalEndPoint as IPEndPoint;
            udpSocket.BeginReceiveFrom(readBuff, 0, readBuff.Length, 0, ref _ep, SocketReceiveFromCallback, null);
        }

        private void SocketReceiveFromCallback(IAsyncResult ar)
        {
            try
            {
                int bytesIn = udpSocket.EndReceiveFrom(ar, ref _ep);

                //Check that the packet was from the server
                if (!receivedFromAddress.Equals(serverUdpEndPoint))
                {
                    udpSocket.BeginReceiveFrom(readBuff, 0, readBuff.Length, 0, ref _ep, SocketReceiveFromCallback, null);
                    return;
                }

                byte[] dg = new byte[bytesIn];
                Buffer.BlockCopy(readBuff, 1, dg, 0, bytesIn);

                HandleDatagram((UdpMessageType)readBuff[0], dg);

                udpSocket.BeginReceiveFrom(readBuff, 0, readBuff.Length, 0, ref _ep, SocketReceiveFromCallback, null);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(ObjectDisposedException))
                    return;

                ISLogger.Write("ISUdpClient: Socket receive error: " + ex.Message);
                udpSocket.BeginReceiveFrom(readBuff, 0, readBuff.Length, 0, ref _ep, SocketReceiveFromCallback, null);

            }
        }

        private void HandleDatagram(UdpMessageType type, byte[] data)
        {
            if (type == UdpMessageType.HostOK)
                HandleHostOK();
            else if (type == UdpMessageType.Input)
                HandleInputData(data);

        }

        private void HandleHostOK()
        {
            SendMessage(UdpMessageType.ClientOK);
            Connected = true;
        }

        private void SendMessage(UdpMessageType message)
        {
            udpSocket.SendTo(new byte[] { (byte)message }, serverUdpEndPoint);
        }

        private void HandleInputData(byte[] input)
        {
            InputReceived?.Invoke(this, input);
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    udpSocket?.Dispose();
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
