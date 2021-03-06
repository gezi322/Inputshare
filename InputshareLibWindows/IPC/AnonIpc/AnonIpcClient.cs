﻿using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Displays;
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
    ///  IPC client using anonymous pipes to communicate between SP and the inputshare service
    /// </summary>
    public class AnonIpcClient : IpcBase
    {
        public event EventHandler<ClipboardDataBase> ClipboardDataReceived;
        public event EventHandler<DisplayConfigRequestedArgs> DisplayConfigRequested;
        public event EventHandler<ClipboardDataBase> DoDragDropReceived;
        public event EventHandler CheckForDropReceived;

        private AnonymousPipeClientStream readPipe;
        private AnonymousPipeClientStream writePipe;

        public AnonIpcClient(string readHandle, string writeHandle, string conName) : base(false, conName, false)
        {
            readPipe = new AnonymousPipeClientStream(PipeDirection.In, readHandle);
            writePipe = new AnonymousPipeClientStream(PipeDirection.Out, writeHandle);
            Start(readPipe, writePipe);
            Write(new IpcMessage(IpcMessageType.IpcClientOK));
        }

        
        protected override void ProcessMessage(IpcMessageType type, byte[] data)
        {
            if (type == IpcMessageType.AnonIpcClipboardData)
            {
                AnonIpcClipboardDataMessage msg = new AnonIpcClipboardDataMessage(data);
                ClipboardDataReceived?.Invoke(this, msg.Data);
            }
            else if (type == IpcMessageType.AnonIpcCheckForDrop)
                CheckForDropReceived?.Invoke(this, null);
            else if (type == IpcMessageType.AnonIpcDisplayConfigRequest)
                HandleDisplayConfigRequest(new IpcMessage(data));
            else if (type == IpcMessageType.AnonIpcDoDragDrop)
            {
                AnonIpcDoDragDropMessage msg = new AnonIpcDoDragDropMessage(data);
                DoDragDropReceived.Invoke(this, msg.DropData);
            }

        }

        private void HandleDisplayConfigRequest(IpcMessage message)
        {
            DisplayConfigRequestedArgs args = new DisplayConfigRequestedArgs();
            DisplayConfigRequested?.Invoke(this, args);

            if(args.Config != null)
            {
                Write(new AnonIpcDisplayConfigMessage(args.Config, message.MessageId));
            }
        }

        protected override void OnConnected()
        {
            base.OnConnected();
        }

        protected override void OnDisconnect(string reason)
        {
            base.OnDisconnect(reason);
        }

        protected override void Dispose(bool disposing)
        {
            readPipe?.Dispose();
            writePipe?.Dispose();
            base.Dispose(disposing);
        }

        public void SendEdgeHit(Edge edge)
        {
            Write(new AnonIpcEdgeHitMessage(edge));
        }

        public void SendDisplayConfig(DisplayConfig config)
        {
            Write(new AnonIpcDisplayConfigMessage(config));
        }

        public void SendLeftMouseUpdate(bool pressed)
        {
            Write(new AnonIpcLMouseStateMessage(pressed));
        }

        public void SendClipboardData(ClipboardDataBase data)
        {
            Write(new AnonIpcClipboardDataMessage(data));
        }


        public void SendDragDropComplete()
        {
            Write(new IpcMessage(IpcMessageType.AnonIpcDragDropComplete));
        }

        public void SendDragDropSuccess()
        {
            Write(new IpcMessage(IpcMessageType.AnonIpcDragDropSuccess));
        }

        public void SendDragDropCancelled()
        {
            Write(new IpcMessage(IpcMessageType.AnonIpcDragDropCancelled));
        }

        public void SendDroppedData(ClipboardDataBase data)
        {
            Write(new AnonIpcDoDragDropMessage(data));
        }

        public async Task<byte[]> ReadStreamAsync(Guid token, Guid fileId, int readLen)
        {
            try
            {
                AnonIpcReadStreamResponseMessage msg = (AnonIpcReadStreamResponseMessage)await SendRequest(new AnonIpcReadStreamRequestMessage(token, fileId, readLen), IpcMessageType.AnonIpcStreamReadResponse);
                return msg.ResponseData;
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to read stream " + ex.Message);
                return new byte[0];
            }
        }
        
        public async Task<Guid> RequestFileTokenAsync(Guid operationId)
        {
            try
            {
                ISLogger.Write("Sending file token request");
                AnonIpcRequestFileTokenResponseMessage msg = (AnonIpcRequestFileTokenResponseMessage)await SendRequest(new AnonIpcRequestFileTokenMessage(operationId), IpcMessageType.AnonIpcFileTokenResponse);
                ISLogger.Write("Got access token response " + msg.AccessToken);
                return msg.AccessToken;
            }
            catch (Exception)
            {
                ISLogger.Write("Failed to get file access token");
                return Guid.Empty;
            }
        }

        public class DisplayConfigRequestedArgs
        {
            public DisplayConfig Config { get; set; }
        }
    }
}
