using Inputshare.Common.Net.UDP.Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Inputshare.Common.Net.UDP
{
    internal class ClientUdpSocket : UdpBase
    {
        internal IPEndPoint BindAddress { get; private set; } = new IPEndPoint(IPAddress.Any, 0);

        private Socket _udpSocket;
        private byte[] _buffer = new byte[512];
        private Thread _readThread;
        private bool _exitLoop = false;
        private IPEndPoint _serverAddress;

        public static ClientUdpSocket Create(IPEndPoint serverAddress, int bindSpecificPort)
        {
            ClientUdpSocket instance = new ClientUdpSocket();
            instance.StartReadSocket(serverAddress, bindSpecificPort);
            return instance;
        }

        private void StartReadSocket(IPEndPoint serverAddress, int bindSpecificPort)
        {
            _serverAddress = serverAddress;
            _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _udpSocket.Bind(new IPEndPoint(IPAddress.Any, bindSpecificPort));
            BindAddress = _udpSocket.LocalEndPoint as IPEndPoint;
            _readThread = new Thread(ReadThreadLoop);
            _readThread.Priority = ThreadPriority.Highest;
            _readThread.Start();
            Logger.Debug($"Created client UDP socket on {_udpSocket.LocalEndPoint} (server address = {serverAddress})");
        }

        private void ReadThreadLoop()
        {
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            while (!_exitLoop)
            {
                try
                {
                    _udpSocket.ReceiveFrom(_buffer, SocketFlags.None, ref sender);
                    HandleDatagram(_buffer, sender as IPEndPoint);
                }catch(Exception) when (_exitLoop)
                {
                    //Ignore errors after disposed
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to read UDP message: " + ex.Message);
                }
            }
        }

        internal override void SendMessage(IUdpMessage message, IPEndPoint address)
        {
            _udpSocket.SendTo(message.ToBytes(), address);
        }

        internal void SendToServer(IUdpMessage message)
        {
            _udpSocket.SendTo(message.ToBytes(), _serverAddress);

        }

        protected override void DisposeSocket()
        {
            _exitLoop = true;
            _udpSocket.Dispose();
        }

      
    }
}
