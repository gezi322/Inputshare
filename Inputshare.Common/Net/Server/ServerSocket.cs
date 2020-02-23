using Inputshare.Common.Input;
using Inputshare.Common.Net.Messages;
using Inputshare.Common.Net.Messages.Replies;
using Inputshare.Common.Net.Messages.Requests;
using Inputshare.Common.Net.RFS;
using Inputshare.Common.Net.UDP;
using Inputshare.Common.Net.UDP.Messages;
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
        internal bool UdpConnected { get; private set; }
        internal IPEndPoint UdpAddress { get; private set; } = new IPEndPoint(IPAddress.Any, 0);

        private ServerUdpSocket _udpSocket;
       

        internal ServerSocket(Socket client, RFSController fileController) : base(fileController)
        {
            Connected = true;
            //Begin receiving data as another task
            BeginReceiveData(client);
            //Send confirmation message to client
            SendMessage(new NetServerConnectionMessage("hello"));
        }

        internal void SetUdpSocket(ServerUdpSocket socket, IPEndPoint udpAddress)
        {
            _udpSocket = socket;
            UdpAddress = udpAddress;
            socket.RegisterHandlerForAddress(udpAddress, HandleUdpMessage);
            _udpSocket.SendMessage(new UdpGenericMessage(UdpMessageType.ServerOK), UdpAddress);
            Logger.Information($"Set UDP address for {Address} ({UdpAddress})");
        }

        private void HandleUdpMessage(IUdpMessage message)
        {
            Logger.Verbose($"Received UDP message {message.GetType().Name}");

            if(message.Type == UdpMessageType.ClientOK && !UdpConnected)
            {
                UdpConnected = true;
                Logger.Information($"{Address.Address}: Udp enabled");
            }
        }

        internal async Task SendSideUpdateAsync(Side[] activeSides)
        {
            bool left = activeSides.Contains(Side.Left);
            bool right = activeSides.Contains(Side.Right);
            bool bottom = activeSides.Contains(Side.Bottom);
            bool top = activeSides.Contains(Side.Top);
            await SendMessageAsync(new NetClientSideStateMessage(left, right, top, bottom));
        }

        /// <summary>
        /// Sends input data to the client
        /// </summary>
        /// <param name="input"></param>
        internal void SendInput(ref InputData input)
        {
            if (UdpConnected)
            {
                _udpSocket.SendMessage(new UdpInputMessage(input), UdpAddress);
            }
            else
            {
                NetMessageHeader header = NetMessageHeader.CreateInputHeader(ref input);
                WriteRawData(header.Data);
            }
            
        }

        internal void NotifyInputClient(bool inputClient)
        {
            SendMessage(new NetInputClientStateMessage(inputClient));
        }

        protected override void HandleException(Exception ex)
        {
            if (Connected)
            {
                Logger.Error($"({Address}) : {ex.Message} \n {ex.StackTrace}");
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
