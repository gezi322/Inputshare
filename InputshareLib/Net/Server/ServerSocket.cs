using InputshareLib.Net.Messages;
using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Net.Server
{
    internal class ServerSocket : SocketBase
    {
        internal event EventHandler<ServerSocket> Disconnected;
        internal event EventHandler<Tuple<Side, int, int>> SideHit;
        internal event EventHandler<Rectangle> DisplayBoundsChanged;

        internal bool Connected { get; private set; } = true;

        private readonly Socket _client;

        internal ServerSocket(Socket client)
        {
            _client = client;
            //Begin receiving data as another task
            BeginReceiveData(_client);
            //Send confirmation message to client
            SendMessage(new NetServerConnectionMessage("hello"));
        }

        internal async Task<byte[]> GetScreenshotAsync()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var reply = await SendRequestAsync<NetScreenshotReply>(new NetScreenshotRequest());
            sw.Stop();
            Logger.Write("Reply time: " + sw.ElapsedMilliseconds + "MS");
            
            return reply.Bmp;
        }

        internal async Task NotifyInputClientAsync(bool inputClient)
        {
            await SendMessageAsync(new NetInputClientStateMessage(inputClient));
        }

        protected override void HandleException(Exception ex)
        {
            Connected = false;
            base.Dispose();
            Disconnected?.Invoke(this, this);
        }

        protected override void HandleGenericMessage(NetMessageBase message)
        {
            if (message is NetSideHitMessage sideHitMsg)
                SideHit?.Invoke(this, new Tuple<Side, int, int>(sideHitMsg.HitSide, sideHitMsg.PosX, sideHitMsg.PosY));
            else if (message is NetDisplayBoundsUpdateMessage displayMsg)
                DisplayBoundsChanged?.Invoke(this, displayMsg.NewBounds);
        }

        protected override async Task HandleRequestAsync(NetRequestBase request)
        {

        }
    }
}
