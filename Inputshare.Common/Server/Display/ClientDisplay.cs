using System;
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

        internal ClientDisplay(ObservableDisplayList displayList, ClientConnectedArgs connectedArgs) : base(displayList, connectedArgs.DisplayBounds, connectedArgs.Name)
        {
            Socket = connectedArgs.Socket;
            Socket.ClipboardDataReceived += (object obj, ClipboardData cbData) => base.OnClipboardChanged(cbData);
            Socket.DisplayBoundsChanged += OnDisplayBoundsReceived;
            Socket.SideHit += (object o, Tuple<Side, int, int> data) => base.OnSideHit(data.Item1, data.Item2, data.Item3);
            Socket.Disconnected += (object o, ServerSocket s) => RemoveDisplay();
            Logger.Information($"Created client display {DisplayName} ({Socket.Address}) ({connectedArgs.DisplayBounds})");
        }

        private void OnDisplayBoundsReceived(object sender, System.Drawing.Rectangle newBounds)
        {
            DisplayBounds = newBounds;
            Logger.Information($"Display bounds for {DisplayName} set to {newBounds}");
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
            Logger.Verbose($"Notifying sides changed update to: {DisplayName}");
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
            Logger.Verbose($"Notifying client active: {DisplayName}");
            Socket.NotifyInputClient(true);
        }

        internal override void NotifyClientInvactive()
        {
            Logger.Verbose($"Notifying client inactive: {DisplayName}");
            Socket.NotifyInputClient(false);
        }

        internal override void RemoveDisplay()
        {
            Logger.Information($"Removing display {DisplayName}");

            base.RemoveDisplay();

            if (Socket.Connected)
                Socket.DisconnectSocket();

            Socket.Dispose();
        }

        internal override async Task SetClipboardAsync(ClipboardData cbData)
        {
            Logger.Debug($"Sending clipboard data to display {DisplayName}");
            await Socket.SendClipboardDataAsync(cbData);
        }
    }
}
