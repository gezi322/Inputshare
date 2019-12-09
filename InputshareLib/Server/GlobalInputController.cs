using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using InputshareLib.Input;
using InputshareLib.Input.Hotkeys;
using InputshareLib.Input.Keys;
using InputshareLib.PlatformModules.Input;

namespace InputshareLib.Server
{
    internal sealed class GlobalInputController
    {
        public ISServerSocket CurrentInputClient { get; private set; } = ISServerSocket.Localhost;
        public event EventHandler<ISServerSocket> InputClientSwitched;

        private ClientManager clientMan;
        private InputManagerBase inputMan;
        private ISUdpServer udpHost;

        private Stopwatch clientSwitchStopwatch = new Stopwatch();

        private bool LocalInput { get { return CurrentInputClient.IsLocalhost; } }

        public GlobalInputController(ClientManager clientManager, InputManagerBase inputManager, ISUdpServer udp)
        {
            udpHost = udp;
            inputMan = inputManager;
            clientMan = clientManager;
            clientSwitchStopwatch.Start();
        }

        public void SetInputClient(ISServerSocket client)
        {
            if(client == null)
            {
                SetInputLocal();
                return;
            }

            if (client.IsLocalhost)
                SetInputLocal();
            else
                SetInputExternal(client);
        }

        public void HandleClientHotkey(Guid clientId)
        {
            //Guid of 0000-0000... means localhost
            if(clientId == Guid.Empty)
            {
                SetInputLocal();
                return;
            }

            if (!clientMan.TryGetClientById(clientId, out ISServerSocket client))
            {
                ISLogger.Write("Client hotkey - Could not find client {0}", clientId);

                try
                {
                    inputMan.RemoveClientHotkey(clientId);
                }
                catch(Exception) { }
                
                return;
            }

            SetInputClient(client);
        }

        public void HandleEdgeHit(ISServerSocket client, Edge edge)
        {
            //Prevent rapid switching when mouse is on border
            if (clientSwitchStopwatch.ElapsedMilliseconds < 50)
                return;

            if (client.IsLocalhost)
                HandleEdgeHitLocal(edge);
            else
                HandleEdgeHitExternal(client, edge);

            clientSwitchStopwatch.Restart();
        }

        public void HandleInputReceived(ISInputData input)
        {
            if (CurrentInputClient != ISServerSocket.Localhost)
            {
                if (CurrentInputClient.IsConnected)
                {
                    if (CurrentInputClient.UdpEnabled && udpHost != null)
                        udpHost.SendInput(input, CurrentInputClient);
                    else
                        CurrentInputClient.SendInputData(input.ToBytes());
                }
                else
                {
                    ISLogger.Write("TARGET NOT CONNECTED");
                }
            }
        }

        private void HandleEdgeHitLocal(Edge edge)
        {
            if (!LocalInput)
                return;

            ISServerSocket target = ISServerSocket.Localhost.GetClientAtEdge(edge);

            if (target == null)
                return;
            else if (!target.IsConnected && !target.IsLocalhost)
            {
                ISServerSocket.Localhost.SetClientAtEdge(edge, null);
                return;
            }

            SetInputExternal(target);
        }

        private void HandleEdgeHitExternal(ISServerSocket client, Edge edge)
        {
            //Ignore if not input client
            if (client != CurrentInputClient)
                return;

            ISServerSocket target = client.GetClientAtEdge(edge);

            if (target == null)
                return;

            if (!target.IsConnected && !target.IsLocalhost)
            {
                client.SetClientAtEdgeNoUpdate(edge, null);
                return;
            }
                

            SetInputClient(target);
        }

        private void SetInputLocal()
        {
            if (!CurrentInputClient.IsLocalhost && CurrentInputClient.IsConnected)
                CurrentInputClient.NotifyActiveClient(false);

            inputMan.SetInputBlocked(false);
            CurrentInputClient = ISServerSocket.Localhost;
            //TODO - reset key states?
            InputClientSwitched?.Invoke(this, ISServerSocket.Localhost);
        }

        private void SetInputExternal(ISServerSocket client)
        {
            if(!CurrentInputClient.IsLocalhost && CurrentInputClient.IsConnected)
                CurrentInputClient.NotifyActiveClient(false);

            inputMan.SetInputBlocked(true);
            CurrentInputClient = client;
            client.NotifyActiveClient(true);
            InputClientSwitched?.Invoke(this, client);
        }
    }
}
