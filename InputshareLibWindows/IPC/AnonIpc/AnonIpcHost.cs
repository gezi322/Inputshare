using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Input;
using InputshareLibWindows.IPC.AnonIpc.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using static InputshareLib.Displays.DisplayManagerBase;

namespace InputshareLibWindows.IPC.AnonIpc
{
    public sealed class AnonIpcHost : AnonIpcBase
    {
        public event EventHandler<DisplayConfig> DisplayConfigUpdated;
        public event EventHandler<Edge> EdgeHit;
        public event EventHandler<bool> LeftMouseStateUpdated;
        public event EventHandler<ClipboardDataBase> DataDropped;
        public event EventHandler<StreamReadRequestArgs> RequestedReadStream;
        public event EventHandler<ClipboardDataBase> ClipboardDataReceived;

        public event EventHandler<Guid> DragDropCancelled;
        public event EventHandler<Guid> DragDropSuccess;
        public event EventHandler<Guid> DragDropComplete;

        public string WriteHandle { get => writeHandle; }
        public string ReadHandle { get => readHandle; }

        public AnonIpcHost(string conName) : base(conName)
        {

        }

        protected override void ProcessMessage(byte[] data)
        {
            AnonIpcMessageType type = (AnonIpcMessageType)data[0];
            switch (type)
            {
                case AnonIpcMessageType.ClientOK:
                    HandleClientOK();
                    break;
                case AnonIpcMessageType.DisplayConfigReply:
                    DisplayConfigUpdated?.Invoke(this, new AnonIpcDisplayConfigMessage(data).Config);
                    break;
                case AnonIpcMessageType.EdgeHit:
                    EdgeHit?.Invoke(this, new AnonIpcEdgeHitMessage(data).HitEdge);
                    break;
                case AnonIpcMessageType.LMouseStateReply:
                    LeftMouseStateUpdated?.Invoke(this, new AnonIpcLMouseStateMessage(data).LeftMouseState);
                    break;
                case AnonIpcMessageType.DoDragDrop:
                    DataDropped?.Invoke(this, new AnonIpcDoDragDropMessage(data).DropData);
                    break;
                case AnonIpcMessageType.DragDropComplete:
                    DragDropComplete?.Invoke(this, new AnonIpcMessage(data).MessageId);
                    break;
                case AnonIpcMessageType.DragDropSuccess:
                    DragDropSuccess?.Invoke(this, new AnonIpcMessage(data).MessageId);
                    break;
                case AnonIpcMessageType.DragDropCancelled:
                    DragDropCancelled?.Invoke(this, new AnonIpcMessage(data).MessageId);
                    break;
                case AnonIpcMessageType.StreamReadRequest:
                    AnonIpcReadStreamRequestMessage msg = new AnonIpcReadStreamRequestMessage(data);
                    RequestedReadStream?.Invoke(this, new StreamReadRequestArgs(msg.MessageId, msg.Token, msg.FileId, msg.ReadLen));
                    break;
                case AnonIpcMessageType.ClipboardData:
                    ClipboardDataReceived?.Invoke(this, new AnonIpcClipboardDataMessage(data).Data);
                    break;
                default:
                    ISLogger.Write("AnonIpcHost: Received unexpected message type " + type);
                    break;
            }
        }

        private byte[] header5 = BitConverter.GetBytes(6);
        public void SendInput(ISInputData input)
        {
            byte[] data = new byte[10];
            Buffer.BlockCopy(header5, 0, data, 0, 4);
            data[4] = (byte)AnonIpcMessageType.InputData;
            Buffer.BlockCopy(input.ToBytes(), 0, data, 5, 5);
            pipeWrite.Write(data);
            pipeWrite.Flush();
        }

        public void SendClipboardData(ClipboardDataBase data)
        {
            Write(new AnonIpcClipboardDataMessage(data));
        }

        public void SendReadReply(Guid messageId, byte[] data)
        {
            Write(new AnonIpcReadStreamResponseMessage(data, messageId));
        }

        public void SendReadReplyError(Guid messageId)
        {
            Write(new AnonIpcMessage(AnonIpcMessageType.StreamReadError, messageId));
        }

        /// <summary>
        /// Gets the display config from the client
        /// </summary>
        /// <returns></returns>
        public DisplayConfig GetDisplayConfig()
        {
            AnonIpcMessage reply = SendRequest(new AnonIpcMessage(AnonIpcMessageType.DisplayConfigRequest));


            if (!(reply is AnonIpcDisplayConfigMessage))
                throw new InvalidResponseException();
            else
                return (reply as AnonIpcDisplayConfigMessage).Config;
        }

        public void SendDoDragDrop(ClipboardDataBase dropData, Guid operationId)
        {
            Write(new AnonIpcDoDragDropMessage(dropData, operationId));
        }

        public void SendCheckForDrop()
        {
            Write(AnonIpcMessageType.CheckForDrop);
        }

        private void HandleClientOK()
        {
            ISLogger.Write("AnonIpcHost ({0}): Client connected", pipeName);
            Write(AnonIpcMessageType.HostOK);
            Connected = true;
        }

        public class StreamReadRequestArgs
        {
            public StreamReadRequestArgs(Guid messageId, Guid token, Guid fileId, int readLen)
            {
                MessageId = messageId;
                Token = token;
                FileId = fileId;
                ReadLen = readLen;
            }

            public Guid MessageId { get; }
            public Guid Token { get; }
            public Guid FileId { get; }
            public int ReadLen { get; }
        }

        public void ReCreatePipeHost()
        {
            Connected = false;
            readBuffer = new byte[1024];
            pipeRead.Dispose();
            pipeWrite.Dispose();
            CreateHost();
        }

        public void RemoveHandles()
        {
            DisposeHandles();
        }
    }
}
