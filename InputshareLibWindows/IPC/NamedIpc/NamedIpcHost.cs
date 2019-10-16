using InputshareLib;
using InputshareLibWindows.IPC.NamedIpc.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Text;
using System.Threading;

namespace InputshareLibWindows.IPC.NamedIpc
{
    public sealed class NamedIpcHost
    {
        public bool ClientConnected { get; private set; }
        private byte[] readBuffer = new byte[1024];

        private NamedPipeServerStream hostStream;
        private Timer pipePollTimer;

        private Dictionary<Guid, Tuple<AutoResetEvent, NamedIpcMessage>> awaitingMessages = new Dictionary<Guid, Tuple<AutoResetEvent, NamedIpcMessage>>();
        private object awaitingMessagesLock = new object();

        public event EventHandler<StateRequestArgs> ConnectionStateRequested;
        public event EventHandler<Tuple<IPEndPoint, string>> CommandConnect;
        public event EventHandler Disconnect;
        public event EventHandler<bool> SetAutoReconnect;

        public NamedIpcHost()
        {
            hostStream = new NamedPipeServerStream("inputsharepipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            ISLogger.Write("NamedIpcHost: waiting for client");
            hostStream.BeginWaitForConnection(WaitForConnectionCallback, null);
            pipePollTimer = new Timer(PipePollTimerCallback, null, 0, 1000);
        }

        private bool lastReportedState = false;
        private void PipePollTimerCallback(object sync)
        {
            bool state = hostStream.IsConnected;

            if(state != lastReportedState)
            {
                if(state == false)
                {
                    OnClientDisconnect();
                }

                lastReportedState = state;
            }
        }

        private void WaitForConnectionCallback(IAsyncResult ar)
        {
            try
            {
                hostStream.EndWaitForConnection(ar);
                ClientConnected = true;
                ISLogger.Write("NamedIpcHost: Client connected");

                hostStream.BeginRead(readBuffer, 0, 4, ReadCallback, null);
            }catch(IOException ex)
            {
                ISLogger.Write("NamedIpcHost: An error occurred while waiting for connection: " + ex.Message);
            }
            catch (ObjectDisposedException) { }
        }

        private int bytesIn;
        private void ReadCallback(IAsyncResult ar)
        {
            try
            {
                bytesIn = hostStream.EndRead(ar);

                do
                {
                    bytesIn += hostStream.Read(readBuffer, bytesIn, 4 - bytesIn);
                } while (bytesIn < 4);

                int header = BitConverter.ToInt32(readBuffer, 0);

                //TODO - temp
                if (header > readBuffer.Length)
                    readBuffer = new byte[header + 4];
                bytesIn = hostStream.Read(readBuffer, 4, header);
                do
                {
                    bytesIn += hostStream.Read(readBuffer, bytesIn + 4, header - bytesIn);
                } while (bytesIn < header);

                try
                {
                    byte[] data = new byte[bytesIn];
                    Buffer.BlockCopy(readBuffer, 4, data, 0, bytesIn);
                    PreProcessMessage(data);
                }catch(Exception ex)
                {
                    ISLogger.Write("NamedIpcHost: An error occurred while processing message: " + ex.Message);
                    ISLogger.Write(ex.StackTrace);
                }
                

                hostStream.BeginRead(readBuffer, 0, 4, ReadCallback, null);
            }
            catch (Exception ex)
            {
                ISLogger.Write("NamedIpcHost: Read error: " + ex.Message);
                OnClientDisconnect();
            }
        }

        private void PreProcessMessage(byte[] data)
        {
            NamedIpcMessage msg = NamedIpcMessageSerializer.DeSerialize(data);

            bool handled = false;
            lock (awaitingMessagesLock)
            {
                
                Guid id = msg.MessageId;

                foreach (var i in awaitingMessages.Values)
                {
                    ISLogger.Write(i.Item2.MessageId);
                }

                if (awaitingMessages.TryGetValue(id, out Tuple<AutoResetEvent, NamedIpcMessage> awaiting))
                {
                    AutoResetEvent evt = awaiting.Item1;

                    awaitingMessages.Remove(id);
                    awaitingMessages.Add(id, new Tuple<AutoResetEvent, NamedIpcMessage>(evt, msg));
                    evt.Set();
                    handled = true;
                }
            }


            if (!handled)
                ProcessMessage(msg);
        }

        private void ProcessMessage(NamedIpcMessage message)
        {
            NamedIpcMessageType type = message.MessageType;

            if (type == NamedIpcMessageType.StateRequest)
                HandleStateRequest(message);
            else if (type == NamedIpcMessageType.Connect)
            {
                NamedIpcConnectMessage msg = message as NamedIpcConnectMessage;
                CommandConnect?.Invoke(this, new Tuple<IPEndPoint, string>(new IPEndPoint(IPAddress.Parse(msg.Address), msg.Port), msg.ClientName));
            }
            else if (type == NamedIpcMessageType.Disconnect)
                Disconnect?.Invoke(this, null);
            else if (type == NamedIpcMessageType.DisableAutoReconnect)
                SetAutoReconnect?.Invoke(this, false);
            else if (type == NamedIpcMessageType.EnableAutoReconnect)
                SetAutoReconnect?.Invoke(this, true);

        }

        public void SendState(bool connected, IPEndPoint address, string clientName, Guid clientId, bool autoReconnect)
        {
            try
            {
                ISLogger.Write("sendstate " + connected);
                if (address == null)
                    Write(new NamedIpcStateResponseMessage(Guid.NewGuid(), connected, "", clientName, clientId, autoReconnect));
                else
                    Write(new NamedIpcStateResponseMessage(Guid.NewGuid(), connected, address.ToString(), clientName, clientId, autoReconnect));

                ISLogger.Write("state sent");
            } catch (Exception ex)
            {
                ISLogger.Write("NamedIpcHost: Failed to send state: " + ex.Message);
            }
          
        }

        private void HandleStateRequest(NamedIpcMessage message)
        {
            ISLogger.Write("Got state request");
            StateRequestArgs args = new StateRequestArgs();
            ConnectionStateRequested(this, args);

            string addr = "";
            if (args.ConnectedAddress != null)
                addr = args.ConnectedAddress.ToString();

            Write(new NamedIpcStateResponseMessage(message.MessageId, args.Connected, addr, args.ClientName, args.clientId, args.AutoReconnectEnabled));
            ISLogger.Write("Responded to state request");
        }

        private void Write(NamedIpcMessageType message)
        {
            Write(new NamedIpcMessage(message));
        }

        private void Write(NamedIpcMessage message)
        {
            Write(NamedIpcMessageSerializer.Serialize(message));
        }

        private void Write(byte[] data)
        {
            try
            {
                hostStream.Write(BitConverter.GetBytes(data.Length));
                hostStream.Write(data);
            }
            catch (Exception ex)
            {
                ISLogger.Write("NamedIpcHost: Write error: " + ex.Message);
                OnClientDisconnect();
            }
        }


        private void OnClientDisconnect()
        {
            try
            {
                hostStream.Disconnect();
            }
            catch { }

            ClientConnected = false;
            ISLogger.Write("NamedIpcHost: Client disconnected");
            hostStream.BeginWaitForConnection(WaitForConnectionCallback, null); 
        }

        public class StateRequestArgs
        {
            public bool Connected { get; set; }
            public IPEndPoint ConnectedAddress { get; set; }
            public string ClientName { get; set; }
            public Guid clientId { get; set; }

            public bool AutoReconnectEnabled { get; set; }

            public StateRequestArgs()
            {

            }
        }
    }
}
