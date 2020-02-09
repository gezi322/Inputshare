using InputshareLib.Input;
using InputshareLib.Net.Server;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace InputshareLib.Server.Displays
{
    internal class ClientDisplay : DisplayBase
    {
        private ServerSocket _socket;

        internal ClientDisplay(ClientConnectedArgs args) : base(args.DisplayBounds, args.Name)
        {
            _socket = args.Socket;
            _socket.DisplayBoundsChanged += Socket_DisplayBoundsChanged;
            _socket.SideHit += (object o, Tuple<Side, int, int> args) => OnSideHit(args.Item1, args.Item2, args.Item3);
            _socket.Disconnected += _socket_Disconnected;
        }

        private void _socket_Disconnected(object sender, ServerSocket e)
        {
            RemoveDisplay();
        }

        private void Socket_DisplayBoundsChanged(object sender, Rectangle bounds)
        {
            OnDisplayBoundsChanged(bounds);
        }

        public override void SendInput(ref InputData input)
        {
            _socket.SendInput(ref input);
        }

        public override async void SetInputDisplay(int newX, int newY)
        {
            var input = new InputData(InputCode.MouseMoveAbsolute, (short)newX, (short)newY);
            _socket.SendInput(ref input);
            InputActive = true;
            await _socket.NotifyInputClientAsync(true);
        }

        public override async void SetNotInputDisplay()
        {
            if (_socket.Connected)
                await _socket.NotifyInputClientAsync(false);
        }
    }
}
