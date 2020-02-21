using Inputshare.Common.Net.UDP.Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Inputshare.Common.Net.Broadcast
{
    public sealed class BroadcastListener : IDisposable
    {
        public event EventHandler<IPEndPoint> BroadcastReceived;

        private UdpClient _listenClient;
        
        /// <summary>
        /// Creates a new instance of a BroadcastListener and starts
        /// listening in the background
        /// </summary>
        /// <returns></returns>
        public static BroadcastListener Create()
        {
            BroadcastListener instance = new BroadcastListener();
            instance._listenClient = new UdpClient(4444);
            instance._listenClient.BeginReceive(instance.ReceiveCallback, null);
            Logger.Write("Waiting for broadcast...");
            return instance;
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                IPEndPoint ipe = new IPEndPoint(IPAddress.Any, 0);

                byte[] data = _listenClient.EndReceive(ar, ref ipe);
                _listenClient.BeginReceive(ReceiveCallback, null);


                IUdpMessage msg = UdpMessageFactory.ReadMessage(data);
                if (msg is UdpServerBroadcastMessage broadcastMessage)
                {
                    if (IPEndPoint.TryParse(broadcastMessage.Address, out var address))
                    {
                        BroadcastReceived?.Invoke(this, address);
                    }
                }
            }
            catch(Exception ex) when (disposedValue)
            {
                //Ignore errors after dispose is called
            }
           
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _listenClient?.Dispose();
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
