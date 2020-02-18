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
        internal bool Connected { get; private set; }

        internal ServerSocket(Socket client)
        {
            Connected = true;
            //Begin receiving data as another task
            BeginReceiveData(client);
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

        internal async Task SendSideUpdateAsync(bool left, bool right, bool top, bool bottom)
        {
            await SendMessageAsync(new NetClientSideStateMessage(left, right, top, bottom));
        }

        internal async Task NotifyInputClientAsync(bool inputClient)
        {
            await SendMessageAsync(new NetInputClientStateMessage(inputClient));
        }

        protected override void HandleException(Exception ex)
        {
            if (Connected)
            {
                Logger.Write($"({Address}) : {ex.Message} \n {ex.StackTrace}");
                Connected = false;
                base.Dispose();
                Disconnected?.Invoke(this, this);
            }
            
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
