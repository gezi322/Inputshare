using InputshareLib.Displays;
using InputshareLib.Input.Hotkeys;
using InputshareLib.Net;
using InputshareLib.Net.Messages;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace InputshareLib.Server
{
    /// <summary>
    /// Represents a client connected to the inputshare server
    /// </summary>
    internal class ISServerSocket : NetworkSocket
    {
        /// <summary>
        /// Dummy client allows localhost to be treated as a client
        /// </summary>
        public static ISServerSocket Localhost = new ISServerSocket(true);

        public static ISServerSocket None = new ISServerSocket(true)
        {
            ClientName = "None",
            ClientId = Guid.NewGuid()
        };

        /// <summary>
        /// Occurs when the client sends an update display configuration
        /// </summary>
        public event EventHandler<DisplayConfig> ClientDisplayConfigUpdated;

        /// <summary>
        /// Occurs when the client sends the required initial info after connecting
        /// </summary>
        public event EventHandler<InitialInfoReceivedArgs> InitialInfoReceived;

        /// <summary>
        /// Fired when the client hits an edge of the screen
        /// </summary>
        public event EventHandler<Edge> EdgeHit;

        /// <summary>
        /// The client below this client
        /// </summary>
        public ISServerSocket BottomClient { get; private set; }
        /// <summary>
        /// The client above this client
        /// </summary>
        public ISServerSocket TopClient { get; private set; }
        /// <summary>
        /// The client to the left of this client
        /// </summary>
        public ISServerSocket LeftClient { get; private set; }
        /// <summary>
        /// The client to the right of this client
        /// </summary>
        public ISServerSocket RightClient { get; private set; }

        /// <summary>
        /// The name of this client
        /// </summary>
        public string ClientName { get; private set; }
        /// <summary>
        /// The GUID of this client
        /// </summary>
        public Guid ClientId { get; private set; }

        /// <summary>
        /// The current display configuration for this client
        /// </summary>
        public DisplayConfig DisplayConfiguration { get; set; }

        /// <summary>
        /// The network address of this client
        /// </summary>
        public IPEndPoint ClientEndpoint { get; private set; }

        /// <summary>
        /// This timer closes the connection if the client
        /// does not send the initial info in time
        /// </summary>
        private Timer initialInfoTimeoutTimer;

        public bool UdpConnected { get; set; }
        public bool UdpEnabled { get; private set; }
        public IPEndPoint UdpAddress { get; private set; }

        /// <summary>
        /// Set to true when the client sends valid initial info.
        /// This prevents the InitialInfoTimeoutTimer from closing
        /// the connection
        /// </summary>
        private bool initialInfoSent = false;

        public readonly bool IsLocalhost = false;

        public ISServerSocket(Socket initSocket) : base(initSocket)
        {
            ClientEndpoint = initSocket.RemoteEndPoint as IPEndPoint;
            initialInfoTimeoutTimer = new Timer(InitialInfoTimeoutCallback, null, 10000, 0);
        }

        /// <summary>
        /// Creates a dummy socket that is assigned to localhost.
        /// </summary>
        /// <param name="LOCALHOSTDUMMY"></param>
        public ISServerSocket(bool LOCALHOSTDUMMY) : base(LOCALHOSTDUMMY)
        {
            ClientName = "Localhost";
            ClientId = Guid.Empty;
            IsLocalhost = true;
        }

        private Hotkey currentHotkey;
        public Hotkey CurrentHotkey { get { return currentHotkey; } set { currentHotkey = value; HotkeyChanged?.Invoke(this, null); } }
        public event EventHandler HotkeyChanged;

        public event EventHandler<Edge> ClientEdgeUpdated;

        /// <summary>
        /// Returns the client at the specified edge of this client.
        /// Returns null if no client is at the specified edge.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public ISServerSocket GetClientAtEdge(Edge edge)
        {
            return edge switch
            {
                Edge.Bottom => BottomClient,
                Edge.Left => LeftClient,
                Edge.Right => RightClient,
                Edge.Top => TopClient,

                _ => throw new ArgumentException("Invalid edge"),
            };
        }

        /// <summary>
        /// Notifes the client if it is the current input client or not
        /// </summary>
        /// <param name="active"></param>
        public void NotifyActiveClient(bool active)
        {
            if (active)
                SendMessage(new NetworkMessage(MessageType.ClientActive));
            else
                SendMessage(new NetworkMessage(MessageType.ClientInactive));
        }

        /// <summary>
        /// Assigns a client to the specified edge of this client
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="client"></param>
        public void SetClientAtEdge(Edge edge, ISServerSocket client)
        {

            switch (edge)
            {
                case Edge.Bottom:
                    BottomClient = client;
                    break;
                case Edge.Left:
                    LeftClient = client;
                    break;
                case Edge.Right:
                    RightClient = client;
                    break;
                case Edge.Top:
                    TopClient = client;
                    break;
            }

            if (client != null)
                ISLogger.Write("Set {0} of {1} to {2}", edge, ClientName, client.ClientName);
            else
                ISLogger.Write("Removed edge {0} of {1}", edge, ClientName);

            ClientEdgeUpdated?.Invoke(this, edge);
        }

        public void SetClientAtEdgeNoUpdate(Edge edge, ISServerSocket client)
        {
            switch (edge)
            {
                case Edge.Bottom:
                    BottomClient = client;
                    break;
                case Edge.Left:
                    LeftClient = client;
                    break;
                case Edge.Right:
                    RightClient = client;
                    break;
                case Edge.Top:
                    TopClient = client;
                    break;
            }

            if (client != null)
                ISLogger.Write("Set {0} of {1} to {2}", edge, ClientName, client.ClientName);
            else
                ISLogger.Write("Removed edge {0} of {1}", edge, ClientName);

        }

        /// <summary>
        /// Tells the client to cancel any current drag/drop operations
        /// </summary>
        public void SendCancelDragDrop()
        {
            SendMessage(new NetworkMessage(MessageType.CancelAnyDragDrop));
        }

        /// <summary>
        /// notifies the client on which edges have clients assigned
        /// </summary>
        public void SendClientEdgesUpdate()
        {
            SendMessage(new ClientEdgesStateMessage(
                TopClient != null, BottomClient != null,
                LeftClient != null, RightClient != null));
        }

        /// <summary>
        /// Fired after the timeout interval for this client.
        /// If the client has not sent valid initialinfo by this time,
        /// the connection is closed.
        /// </summary>
        /// <param name="sync"></param>
        private void InitialInfoTimeoutCallback(object sync)
        {
            if (!initialInfoSent)
                HandleConnectionClosed("Client did not send connection info in time");

            initialInfoTimeoutTimer.Dispose();
        }

        /// <summary>
        /// Sends an input to the client
        /// Inputdata is sent differently than the rest of the messages, to create as little overhead as possible
        /// </summary>
        /// <param name="code"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        public void SendInputData(byte[] inputData)
        {
            byte[] data = new byte[10];
            Buffer.BlockCopy(inputDataHeader, 0, data, 0, 4);
            data[4] = (byte)MessageType.InputData;
            Buffer.BlockCopy(inputData, 0, data, 5, inputData.Length);
            SendRawData(data);
        }
        private byte[] inputDataHeader = BitConverter.GetBytes(10);

        /// <summary>
        /// Sends a serverOK message to let the client know that it is properly connected
        /// </summary>
        public void AcceptClient()
        {
            SendMessage(new NetworkMessage(MessageType.ServerOK));
        }

        /// <summary>
        /// Returns the name of this client
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (ClientName != null)
                return ClientName;
            else
                return base.ToString();
        }

        /// <summary>
        /// Requests the client to send its initial connection info
        /// </summary>
        public void RequestInitialInfo()
        {
            SendMessage(new NetworkMessage(MessageType.ServerRequestInitialInfo));
        }

        /// <summary>
        /// notifies the client that the connection has been declined due to the
        /// specified reason
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="clientVer"></param>
        public void DeclineClient(ClientDeclinedReason reason, string clientVer = null)
        {
            switch (reason)
            {
                case ClientDeclinedReason.DuplicateGuid:
                    SendMessage(new ClientDeclinedMessage("Guid in use"));
                    break;
                case ClientDeclinedReason.DuplicateName:
                    SendMessage(new ClientDeclinedMessage("Name in use"));
                    break;
                case ClientDeclinedReason.MaxClientsReached:
                    SendMessage(new ClientDeclinedMessage("Max clients reached"));
                    break;
                case ClientDeclinedReason.VersionMismatch:
                    if (clientVer != null)
                        SendMessage(new ClientDeclinedMessage(string.Format("Version mismatch (client: {0} | server: {1}", clientVer, Settings.InputshareVersion)));
                    else
                        SendMessage(new ClientDeclinedMessage("Version mismatch"));
                    break;
                case ClientDeclinedReason.Other:
                    SendMessage(new ClientDeclinedMessage("Unknown reason"));
                    break;
            }
        }

        /// <summary>
        /// Closes the socket and optionally notifies the client
        /// </summary>
        /// <param name="notify"></param>
        public void Close(CloseNotifyMode notify)
        {
            if (notify == CloseNotifyMode.ServerStopped)
            {
                SendMessage(new NetworkMessage(MessageType.ServerStopping));
            }

            base.Close();
        }

        /// <summary>
        /// Handles messages received by the client
        /// </summary>
        /// <param name="data"></param>
        protected override void HandleMessage(byte[] data)
        {
            base.HandleMessage(data);
            MessageType type = (MessageType)data[4];

            if (type == MessageType.ClientInitialInfo)
            {
                HandleInitialInfoMessage(new ClientInitialMessage(data));
            }
            else if (type == MessageType.DisplayConfig)
            {
                HandleDisplayConfigMessage(new DisplayConfigMessage(data));
            }
            else if (type >= MessageType.EdgeHitTop && type <= MessageType.EdgeHitLeft)
            {
                HandleEdgeHit(type);
            }
        }

        public void SetUdpEnabled(bool enable)
        {
            if (enable)
            {
                if (!UdpConnected)
                {
                    ISLogger.Write("Failed to enable UDP for client {0}: UDP not connected", this);
                    return;
                }
            }

            UdpEnabled = enable;
            ISLogger.Write("{0}: Udp enabled = {1}", this, enable);
        }

        /// <summary>
        /// Called when the client sends an edge hit message
        /// </summary>
        /// <param name="type"></param>
        private void HandleEdgeHit(MessageType type)
        {
            switch (type)
            {
                case MessageType.EdgeHitLeft:
                    EdgeHit?.Invoke(this, Edge.Left);
                    break;
                case MessageType.EdgeHitRight:
                    EdgeHit?.Invoke(this, Edge.Right);
                    break;
                case MessageType.EdgeHitBottom:
                    EdgeHit?.Invoke(this, Edge.Bottom);
                    break;
                case MessageType.EdgeHitTop:
                    EdgeHit?.Invoke(this, Edge.Top);
                    break;
            }
        }

        /// <summary>
        /// Called when the client sends its initial connection info
        /// </summary>
        /// <param name="message"></param>
        private void HandleInitialInfoMessage(ClientInitialMessage message)
        {
            ClientName = message.ClientName;
            ClientId = message.ClientId;

            if (message.UdpPort != 0)
            {
                UdpAddress = new IPEndPoint(ClientEndpoint.Address, message.UdpPort);
                ISLogger.Write("Client {0} UDP address: {1}", ClientName, UdpAddress);
            }

            try
            {
                DisplayConfiguration = new DisplayConfig(message.DisplayConfig);
            }
            catch (Exception)
            {
                HandleConnectionClosed("Invalid display data");
                return;
            }


            initialInfoSent = true;
            InitialInfoReceived?.Invoke(this, new InitialInfoReceivedArgs(message.ClientName, message.ClientId, message.DisplayConfig, message.Version));
        }

        /// <summary>
        /// Called when the client sends an update of its display configuration
        /// </summary>
        /// <param name="message"></param>
        private void HandleDisplayConfigMessage(DisplayConfigMessage message)
        {
            DisplayConfiguration = new DisplayConfig(message.ConfigData);
            ClientDisplayConfigUpdated?.Invoke(this, DisplayConfiguration);
        }

        public enum ClientDeclinedReason
        {
            DuplicateName = 1,
            DuplicateGuid = 2,
            MaxClientsReached = 3,
            VersionMismatch = 4,
            Other = 5,
        }

        public class InitialInfoReceivedArgs
        {
            public InitialInfoReceivedArgs(string name, Guid id, byte[] displayConf, string clientVer)
            {
                Name = name;
                Id = id;
                DisplayConf = displayConf;
                ClientVer = clientVer;
            }

            public string Name { get; }
            public Guid Id { get; }
            public byte[] DisplayConf { get; }
            public string ClientVer { get; }
        }
    }
}
