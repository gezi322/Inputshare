using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Input;
using InputshareLib.Net.Server;

namespace InputshareLib.Server.Display
{
    public class ClientDisplay : DisplayBase
    {
        private ServerSocket _socket;

        internal ClientDisplay(ClientConnectedArgs connectedArgs) : base(connectedArgs.DisplayBounds, connectedArgs.Name)
        {
            _socket = connectedArgs.Socket;
            _socket.SideHit += (object o, Tuple<Side, int, int> data) => OnSideHit(data.Item1, data.Item2, data.Item3);
            _socket.Disconnected += (object o, ServerSocket s) => RemoveDisplay();
        }

        protected override void SendSideChanged()
        {
            if (!_socket.Connected)
                return;

           _socket.SendSideUpdateAsync(GetDisplayAtSide(Side.Left) != null,
                GetDisplayAtSide(Side.Right) != null,
                GetDisplayAtSide(Side.Top) != null,
                GetDisplayAtSide(Side.Bottom) != null
                );
        }

        internal override void SendInput(ref InputData input)
        {
            if(_socket.Connected)
                _socket.SendInput(ref input);
        }

        internal override void SetInputActive()
        {
            _socket.NotifyInputClientAsync(true);
        }

        internal override void SetInputInactive()
        {
            _socket.NotifyInputClientAsync(false);
        }

        internal override void RemoveDisplay()
        {
            base.RemoveDisplay();

            if(_socket.Connected)
                _socket.DisconnectSocket();
        }
    }
}
