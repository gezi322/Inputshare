using Inputshare.Common.Net.UDP.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Inputshare.Common.Net.UDP
{
    internal abstract class UdpBase : IDisposable
    {
        internal delegate void UdpMessageHandler(IUdpMessage message);

        protected Dictionary<IPEndPoint, UdpMessageHandler> handlers = new Dictionary<IPEndPoint, UdpMessageHandler>();

        internal void RegisterHandlerForAddress(IPEndPoint address, UdpMessageHandler handler)
        {
            try
            {
                handlers.Add(address, handler);
            }catch(Exception ex)
            {
                Logger.Write($"Failed to create UDP handler for {address}: {ex.Message}");
            }
            
        }

        internal void RemoveHandlersForAddress(IPEndPoint address)
        {
            var matches = handlers.Where(i => i.Key == address).ToArray();

            foreach (var addr in matches){
                handlers.Remove(addr.Key);
            }
                
        }

        protected void HandleDatagram(byte[] dg, IPEndPoint sender)
        {
            try
            {
                UdpMessageType type = (UdpMessageType)dg[0];

                if (handlers.TryGetValue(sender, out var handler))
                    handler(UdpMessageFactory.ReadMessage(dg));
                else
                    Logger.Write("No handler found for " + sender.ToString());
            }
            catch(Exception ex)
            {
                Logger.Write("Failed to handle UDP message: " + ex.Message);
            }
        }

        internal abstract void SendMessage(IUdpMessage message, IPEndPoint address);

        private bool disposedValue = false; // To detect redundant calls

        protected abstract void DisposeSocket();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DisposeSocket();
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
