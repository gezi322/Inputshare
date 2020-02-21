﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Inputshare.Common.Clipboard;
using Inputshare.Common.Input;
using Inputshare.Common.Net.Server;
using Inputshare.Common.Net.UDP;
using Inputshare.Common.Net.UDP.Messages;

namespace Inputshare.Common.Server.Display
{
    /// <summary>
    /// Represents the virtual display of a client
    /// </summary>
    public class ClientDisplay : DisplayBase
    {
        internal readonly ServerSocket Socket;
        public bool UdpConnected { get; private set; }

        internal ClientDisplay(ClientConnectedArgs connectedArgs) : base(connectedArgs.DisplayBounds, connectedArgs.Name)
        {
            Socket = connectedArgs.Socket;
            Socket.ClipboardDataReceived += (object obj, ClipboardData cbData) => base.OnClipboardChanged(cbData);
            Socket.SideHit += (object o, Tuple<Side, int, int> data) => base.OnSideHit(data.Item1, data.Item2, data.Item3);
            Socket.Disconnected += (object o, ServerSocket s) => RemoveDisplay();
        }

        protected override async Task SendSideChangedAsync()
        {
            if (!Socket.Connected)
                return;

            Side[] activeSides = new Side[4];
            activeSides[0] = GetDisplayAtSide(Side.Left) == null ? 0 : Side.Left;
            activeSides[1] = GetDisplayAtSide(Side.Right) == null ? 0 : Side.Right;
            activeSides[2] = GetDisplayAtSide(Side.Bottom) == null ? 0 : Side.Bottom;
            activeSides[3] = GetDisplayAtSide(Side.Top) == null ? 0 : Side.Top;
            await Socket.SendSideUpdateAsync(activeSides);
            await base.SendSideChangedAsync();
        }

        internal override void SendInput(ref InputData input)
        {
            if (Socket.Connected)
            {
                if (UdpConnected)
                {

                }
                else
                {
                    Socket.SendInput(ref input);
                }
            }
                
        }

        internal override void NotfyInputActive()
        {
            Socket.NotifyInputClient(true);
        }

        internal override void NotifyClientInvactive()
        {
            Socket.NotifyInputClient(false);
        }

        internal override void RemoveDisplay()
        {
            base.RemoveDisplay();

            if (Socket.Connected)
                Socket.DisconnectSocket();

            Socket.Dispose();

        }

        internal override async Task SetClipboardAsync(ClipboardData cbData)
        {
            await Socket.SendClipboardDataAsync(cbData);
        }
    }
}
