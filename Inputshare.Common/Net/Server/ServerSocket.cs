﻿using Inputshare.Common.Net.Messages;
using Inputshare.Common.Net.Messages.Replies;
using Inputshare.Common.Net.Messages.Requests;
using Inputshare.Common.Net.RFS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Common.Net.Server
{
    internal class ServerSocket : SocketBase
    {
        internal event EventHandler<ServerSocket> Disconnected;
        internal event EventHandler<Tuple<Side, int, int>> SideHit;
        internal event EventHandler<Rectangle> DisplayBoundsChanged;
        internal bool Connected { get; private set; }

        internal ServerSocket(Socket client, RFSController fileController) : base(fileController)
        {
            Connected = true;
            //Begin receiving data as another task
            BeginReceiveData(client);
            //Send confirmation message to client
            SendMessage(new NetServerConnectionMessage("hello"));
        }
        internal async Task SendSideUpdateAsync(Side[] activeSides)
        {
            bool left = activeSides.Contains(Side.Left);
            bool right = activeSides.Contains(Side.Right);
            bool bottom = activeSides.Contains(Side.Bottom);
            bool top = activeSides.Contains(Side.Top);
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

        protected override void HandleRequest(NetRequestBase request)
        {

        }
    }
}