using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using InputshareLib.Input;
using InputshareLib.Net.Server;

namespace InputshareLib.Server.Display
{
    public class ClientDisplay : DisplayBase
    {
        internal readonly ServerSocket Socket;

        internal ClientDisplay(ClientConnectedArgs connectedArgs) : base(connectedArgs.DisplayBounds, connectedArgs.Name)
        {
            Socket = connectedArgs.Socket;
            Socket.SideHit += (object o, Tuple<Side, int, int> data) => OnSideHit(data.Item1, data.Item2, data.Item3);
            Socket.Disconnected += (object o, ServerSocket s) => RemoveDisplay();
        }

        protected override async Task SendSideChangedAsync()
        {
            if (!Socket.Connected)
                return;

           await Socket.SendSideUpdateAsync(GetDisplayAtSide(Side.Left) != null,
                GetDisplayAtSide(Side.Right) != null,
                GetDisplayAtSide(Side.Top) != null,
                GetDisplayAtSide(Side.Bottom) != null
                );

            await base.SendSideChangedAsync();
        }

        internal override void SendInput(ref InputData input)
        {
            if(Socket.Connected)
                Socket.SendInput(ref input);
        }

        internal override async Task NotfyInputActiveAsync()
        {
            await Socket.NotifyInputClientAsync(true);
        }

        internal override async Task NotifyClientInvactiveAsync()
        {
            await Socket.NotifyInputClientAsync(false);
        }

        internal override void RemoveDisplay()
        {
            base.RemoveDisplay();

            if (Socket.Connected)
                Socket.DisconnectSocket();

        }
    }
}
