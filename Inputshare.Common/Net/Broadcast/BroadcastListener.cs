using Inputshare.Common.Net.UDP.Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Inputshare.Common.Net.Broadcast
{
    internal sealed class BroadcastListener : IDisposable
    {
        public event EventHandler<BroadcastReceivedArgs> BroadcastReceived;

        private UdpClient _listenClient;
        
        /// <summary>
        /// Creates a new instance of a BroadcastListener and starts
        /// listening in the background
        /// </summary>
        /// <returns></returns>
        public static BroadcastListener Create(int listenPort)
        {

            BroadcastListener instance = new BroadcastListener
            {
                _listenClient = new UdpClient(listenPort)
            };

            instance._listenClient.BeginReceive(instance.ReceiveCallback, null);
            Logger.Debug($"Waiting for broadcast messages on port {listenPort}");
            return instance;
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                IPEndPoint ipe = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _listenClient.EndReceive(ar, ref ipe);
                _listenClient.BeginReceive(ReceiveCallback, null);

                Logger.Verbose($"BroadcastListener: Received {data.Length} bytes from {ipe}");

                //Read the udp message
                IUdpMessage msg = UdpMessageFactory.ReadMessage(data);
                Logger.Verbose($"BroadcastListener: Message type = {msg.GetType().Name}");
                //Check that it is a valid message
                if (msg is UdpServerBroadcastMessage broadcastMessage)
                {
                    //check that the message address is valid
                    if (IPEndPoint.TryParse(broadcastMessage.Address, out var address))
                    {
                        Ping p = new Ping();
                        //ping the address to make sure that we can connect to it
                        var reply = p.Send(address.Address, 500);
                        Logger.Verbose($"BroadcastListener: Got ping reply from {reply.Address} ({reply.RoundtripTime}MS)");
                        BroadcastReceived?.Invoke(this, new BroadcastReceivedArgs(address, reply, broadcastMessage.ServerVersion));
                    }
                    else
                    {
                        Logger.Verbose($"BroadcastListener: Message contained invalid address");
                    }
                }
            }
            catch(Exception) when (disposedValue)
            {
                //Ignore errors after dispose is called
            }catch(Exception ex)
            {
                Logger.Warning("Failed to read UDP broadcast: " + ex.Message);
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
