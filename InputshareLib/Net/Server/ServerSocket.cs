using InputshareLib.Net.Messages;
using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Net.Server
{
    internal class ServerSocket : SocketBase
    {
        internal event EventHandler<ServerSocket> Disconnected;
        private readonly Socket _client;

        internal ServerSocket(Socket client)
        {
            _client = client;
            //Begin receiving data as another task
            BeginReceiveData(_client);
            //Send confirmation message to client
            SendMessage(new NetServerConnectionMessage("hello"));
        }

        protected override void HandleException(Exception ex)
        {
            base.Dispose();
            Disconnected?.Invoke(this, this);
        }

        protected override void HandleGenericMessage(NetMessageBase message)
        {

        }

        protected override async Task HandleRequestAsync(NetRequestBase request)
        {

        }
    }
}
