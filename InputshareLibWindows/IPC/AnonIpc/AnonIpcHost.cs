using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Displays;
using InputshareLib.Input;
using InputshareLibWindows.IPC.AnonIpc.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace InputshareLibWindows.IPC.AnonIpc
{

    /// <summary>
    /// IPC host using anonymous pipes to communicate between the inputshare proccess and SP.
    /// </summary>
    public class AnonIpcHost : IpcBase
    {
        public event EventHandler<ClipboardDataBase> ClipboardDataReceived;
        public event EventHandler<Edge> EdgeHit;
        public event EventHandler<DisplayConfig> DisplayConfigUpdated;
        public event EventHandler<bool> LeftMouseStateUpdated;
        public event EventHandler<ClipboardDataBase> DataDropped;
        public event EventHandler<Guid> DragDropComplete;
        public event EventHandler<Guid> DragDropCancelled;
        public event EventHandler<Guid> DragDropSuccess;
        public event EventHandler<StreamReadRequestArgs> RequestedReadStream;

        public string ReadStringHandle { get; private set; }
        public string WriteStringHandle { get; private set; }

        private AnonymousPipeServerStream readPipe;
        private AnonymousPipeServerStream writePipe;

        public AnonIpcHost(string conName) : base(false, conName, false)
        {
            RebuildHost();
        }

        private void RebuildHost()
        {
            readPipe?.Close();
            writePipe?.Close();

            readPipe = new AnonymousPipeServerStream(PipeDirection.In, System.IO.HandleInheritability.Inheritable);
            writePipe = new AnonymousPipeServerStream(PipeDirection.Out, System.IO.HandleInheritability.Inheritable);
            ReadStringHandle = readPipe.GetClientHandleAsString();
            WriteStringHandle = writePipe.GetClientHandleAsString();

            Start(readPipe, writePipe);
        }

        protected override void OnConnected()
        {
            base.OnConnected();
        }

        protected override void OnDisconnect(string reason)
        {
            base.OnDisconnect(reason);
        }

        protected override void ProcessMessage(IpcMessageType type, byte[] data)
        {
            if (type == IpcMessageType.AnonIpcClipboardData)
                ClipboardDataReceived?.Invoke(this, new AnonIpcClipboardDataMessage(data).Data);
            else if (type == IpcMessageType.AnonIpcEdgeHit)
                EdgeHit?.Invoke(this, new AnonIpcEdgeHitMessage(data).HitEdge);
            else if (type == IpcMessageType.AnonIpcDisplayConfigReply)
                DisplayConfigUpdated?.Invoke(this, new AnonIpcDisplayConfigMessage(data).Config);
            else if (type == IpcMessageType.AnonIpcLMouseStateReply)
                LeftMouseStateUpdated?.Invoke(this, new AnonIpcLMouseStateMessage(data).LeftMouseState);
            else if (type == IpcMessageType.AnonIpcDoDragDrop)
                DataDropped?.Invoke(this, new AnonIpcDoDragDropMessage(data).DropData);
            else if (type == IpcMessageType.AnonIpcDragDropCancelled)
                DragDropCancelled?.Invoke(this, new IpcMessage(data).MessageId);
            else if (type == IpcMessageType.AnonIpcDragDropSuccess)
                DragDropSuccess?.Invoke(this, new IpcMessage(data).MessageId);
            else if (type == IpcMessageType.AnonIpcDragDropComplete)
                DragDropComplete?.Invoke(this, new IpcMessage(data).MessageId);
            else if (type == IpcMessageType.AnonIpcStreamReadRequest)
                HandleReadStreamRequest(new AnonIpcReadStreamRequestMessage(data));
        }

        private void HandleReadStreamRequest(AnonIpcReadStreamRequestMessage message)
        {
            StreamReadRequestArgs args = new StreamReadRequestArgs(message.MessageId, message.Token, message.FileId, message.ReadLen);
            RequestedReadStream?.Invoke(this, args);
        }

        public void SendClipboardData(ClipboardDataBase data)
        {
            Write(new AnonIpcClipboardDataMessage(data));
        }

        public async Task<DisplayConfig> GetDisplayConfig()
        {
            AnonIpcDisplayConfigMessage msg = (AnonIpcDisplayConfigMessage)await SendRequest(new IpcMessage(IpcMessageType.AnonIpcDisplayConfigRequest), IpcMessageType.AnonIpcDisplayConfigReply);
            return msg.Config;
        }

        public void SendCheckForDrop()
        {
            Write(new IpcMessage(IpcMessageType.AnonIpcCheckForDrop));
        }

        public void SendDoDragDrop(ClipboardDataBase data, Guid operationId)
        {
            Write(new AnonIpcDoDragDropMessage(data, operationId));
        }

        public void SendReadReplyError(Guid messageId)
        {
            Write(new IpcMessage(IpcMessageType.AnonIpcStreamReadError, messageId));
        }

        public void SendReadReply(Guid messageId, byte[] data)
        {
            Write(new AnonIpcReadStreamResponseMessage(data, messageId));
        }

        protected override void Dispose(bool disposing)
        {
            readPipe?.Dispose();
            writePipe?.Dispose();
            base.Dispose(disposing);
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
    }
}
