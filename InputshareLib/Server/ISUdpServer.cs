using InputshareLib.Input;
using InputshareLib.Net.Udp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace InputshareLib.Server
{
    internal class ISUdpServer : IDisposable
    {
        public bool SocketBound { get; private set; }

        private ClientManager clientMan;
        private Socket udpSocket;

        private byte[] readBuff = new byte[4096];
        private EndPoint _recvAddrBuffer = new IPEndPoint(IPAddress.Any, 0);

        private Timer udpCheckTimer;
        internal ISUdpServer(ClientManager clientManager, int bindPort)
        {
            clientMan = clientManager;
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, bindPort));
            udpSocket.BeginReceiveFrom(readBuff, 0, readBuff.Length, 0, ref _recvAddrBuffer, SocketReceiveFromCallback, null);
            udpCheckTimer = new Timer(UdpCheckTimerCallback, null, 0, 4000);
            SocketBound = true;
        }

        private void UdpCheckTimerCallback(object sync)
        {
            //Checks for any client that we have not had a UDP response from. incase the first response packet got lost.
            foreach(var client in clientMan.AllClients.Where(c =>!c.IsLocalhost && !c.UdpConnected && c.UdpAddress != null))
            {
                SendMessage(UdpMessageType.HostOK, client);
            }
        }

         private void SocketReceiveFromCallback(IAsyncResult ar)
        {
            try
            {
                int bytesIn = udpSocket.EndReceiveFrom(ar, ref _recvAddrBuffer);
                byte[] dg = new byte[bytesIn];
                Buffer.BlockCopy(readBuff, 1, dg, 0, bytesIn-1);
                HandleDatagram((UdpMessageType)readBuff[0], dg, _recvAddrBuffer as IPEndPoint);

                udpSocket.BeginReceiveFrom(readBuff, 0, readBuff.Length, 0, ref _recvAddrBuffer, SocketReceiveFromCallback, null);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(ObjectDisposedException))
                    return;

                udpSocket.BeginReceiveFrom(readBuff, 0, readBuff.Length, 0, ref _recvAddrBuffer, SocketReceiveFromCallback, null);
            }
        }

        private void HandleDatagram(UdpMessageType type, byte[] data, IPEndPoint senderAddress)
        {
            var sender = clientMan.GetClientFromUdpAddress(senderAddress);
            
            if(sender == null)
            {
                ISLogger.Write("ISUdpServer: Ignoring UDP packet from unknown address {0}", senderAddress);
                return;
            }

            if (type == UdpMessageType.ClientOK)
                HandleClientOK(sender);
        }

        public void SendInput(ISInputData input, ISServerSocket client)
        {
            byte[] data = new byte[6];
            data[0] = (byte)UdpMessageType.Input;

            input.ToBytes(data, 1);
            udpSocket.BeginSendTo(data, 0, data.Length, 0, client.UdpAddress, SendToCallback, null);
        }

        public void InitClient(ISServerSocket client)
        {
            SendMessage(UdpMessageType.HostOK, client);
        }

        private void HandleClientOK(ISServerSocket sender)
        {
            ISLogger.Write("ISUdpServer: {0} UDP connected", sender);
            sender.UdpConnected = true;
            sender.SetUdpEnabled(true);
        }

        private void SendMessage(UdpMessageType message, ISServerSocket client)
        {
            if (client.UdpAddress == null)
                return;

            udpSocket.SendTo(new byte[] { (byte)message }, client.UdpAddress);
        }

        private void SendToCallback(IAsyncResult ar)
        {
            try
            {
                udpSocket.EndSendTo(ar);
            }catch(Exception ex)
            {
                ISLogger.Write("ISUdpServer: Send error {0}", ex.Message);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    udpCheckTimer?.Dispose();
                    udpSocket?.Dispose();
                    SocketBound = false;
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
