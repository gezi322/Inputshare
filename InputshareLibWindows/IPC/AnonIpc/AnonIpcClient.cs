using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Input;
using InputshareLibWindows.IPC.AnonIpc.Messages;
using System;
using static InputshareLib.Displays.DisplayManagerBase;

namespace InputshareLibWindows.IPC.AnonIpc
{
    public sealed class AnonIpcClient : AnonIpcBase
    {
        public event EventHandler<Guid> DisplayConfigRequested;
        public event EventHandler<Guid> LeftMouseStateRequested;
        public event EventHandler<ISInputData> InputReceived;
        public event EventHandler<Tuple<ClipboardDataBase, Guid>> DoDragDropReceived;
        public event EventHandler CheckForDropReceived;
        public event EventHandler<ClipboardDataBase> ClipboardDataReceived;

        public AnonIpcClient(string writeHandle, string readHandle, string conName) : base(writeHandle, readHandle, conName)
        {
            Write(AnonIpcMessageType.ClientOK);
        }

        protected override void ProcessMessage(byte[] data)
        {
            AnonIpcMessageType type = (AnonIpcMessageType)data[0];
            switch (type)
            {
                case AnonIpcMessageType.InputData:
                    HandleInputData(data);
                    break;
                case AnonIpcMessageType.HostOK:
                    HandleHostOK();
                    break;
                case AnonIpcMessageType.DisplayConfigRequest:
                    DisplayConfigRequested?.Invoke(this, new AnonIpcMessage(data).MessageId);
                    break;
                case AnonIpcMessageType.LMouseStateRequest:
                    LeftMouseStateRequested?.Invoke(this, new AnonIpcMessage(data).MessageId);
                    break;
                case AnonIpcMessageType.DoDragDrop:
                    AnonIpcDoDragDropMessage msg = new AnonIpcDoDragDropMessage(data);
                    DoDragDropReceived?.Invoke(this, new Tuple<ClipboardDataBase, Guid>(msg.DropData, msg.MessageId));
                    break;
                case AnonIpcMessageType.CheckForDrop:
                    CheckForDropReceived?.Invoke(this, new EventArgs());
                    break;
                case AnonIpcMessageType.ClipboardData:
                    ClipboardDataReceived?.Invoke(this, new AnonIpcClipboardDataMessage(data).Data);
                    break;
                default:
                    ISLogger.Write("AnonIpcClient: Received unexpected message type " + type);
                    break;
            }
        }

        private void HandleInputData(byte[] data)
        {
            byte[] d = new byte[5];
            Buffer.BlockCopy(data, 1, d, 0, 5);
            InputReceived?.Invoke(this, new ISInputData(d));
        }

        /// <summary>
        /// Read a remote file stream
        /// </summary>
        /// <param name="token"></param>
        /// <param name="fileId"></param>
        /// <param name="bytesRead"></param>
        /// <returns></returns>
        /// <exception cref="AnonIpcBase.InvalidResponseException"></exception>"
        /// <exception cref="AnonIpcBase.RemoteStreamReadException"></exception>"
        public byte[] ReadStream(Guid token, Guid fileId, int bytesRead)
        {
            AnonIpcMessage response = SendRequest(new AnonIpcReadStreamRequestMessage(token, fileId, bytesRead));

            if (response.MessageType == AnonIpcMessageType.StreamReadResponse)
                return (response as AnonIpcReadStreamResponseMessage).ResponseData;
            else if (response.MessageType == AnonIpcMessageType.StreamReadError)
                throw new RemoteStreamReadException();
            else
                throw new InvalidResponseException();
        }

        public void SendClipboardData(ClipboardDataBase data)
        {
            Write(new AnonIpcClipboardDataMessage(data));
        }

        public void SendDroppedData(ClipboardDataBase data)
        {
            Write(new AnonIpcDoDragDropMessage(data));
        }

        public void SendDragDropCancelled(Guid operationId)
        {
            Write(new AnonIpcMessage(AnonIpcMessageType.DragDropCancelled, operationId));
        }
        public void SendDragDropSuccess(Guid operationId)
        {
            Write(new AnonIpcMessage(AnonIpcMessageType.DragDropSuccess, operationId));
        }
        public void SendDragDropComplete(Guid operationId)
        {
            Write(new AnonIpcMessage(AnonIpcMessageType.DragDropComplete, operationId));
        }

        public void SendEdgeHit(Edge edge)
        {
            Write(new AnonIpcEdgeHitMessage(edge));
        }

        public void SendDisplayConfigUpdate(DisplayConfig config)
        {
            Write(new AnonIpcDisplayConfigMessage(config));
        }
        public void SendDisplayConfigReply(Guid messageId, DisplayConfig config)
        {
            Write(new AnonIpcDisplayConfigMessage(config, messageId));
        }

        public void SendLeftMouseState(bool state)
        {
            Write(new AnonIpcLMouseStateMessage(state));
        }

        private void HandleHostOK()
        {
            Connected = true;
        }

        
    }
}
