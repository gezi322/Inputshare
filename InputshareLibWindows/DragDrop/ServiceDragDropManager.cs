using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.DragDrop;
using InputshareLibWindows.IPC.AnonIpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.DragDrop
{
    public class ServiceDragDropManager : IDragDropManager
    {
        private AnonIpcHost dropHost;
        private AnonIpcHost mainHost;

        public ServiceDragDropManager(AnonIpcHost hostMain, AnonIpcHost hostDragDrop)
        {
            dropHost = hostDragDrop;
            mainHost = hostMain;

            mainHost.LeftMouseStateUpdated += (object s, bool state) => { LeftMouseState = state; };
            dropHost.DataDropped += (object s, ClipboardDataBase data) => { DataDropped?.Invoke(this, data); };
            dropHost.DragDropCancelled += (object s, Guid id) => { DragDropCancelled?.Invoke(this, id); };
            dropHost.DragDropComplete += (object s, Guid id) => { DragDropComplete?.Invoke(this, id); };
            dropHost.DragDropSuccess += (object s, Guid id) => { DragDropSuccess?.Invoke(this, id); };
            dropHost.RequestedReadStream += (object s, AnonIpcHost.StreamReadRequestArgs args) => { FileDataRequested?.Invoke(this, new IDragDropManager.RequestFileDataArgs(args.MessageId, args.Token, args.FileId, args.ReadLen)); };
        }

        public bool Running { get => true; }

        public bool LeftMouseState { get; private set; } = false;

        public event EventHandler<Guid> DragDropCancelled;
        public event EventHandler<Guid> DragDropSuccess;
        public event EventHandler<Guid> DragDropComplete;
        public event EventHandler<ClipboardDataBase> DataDropped;
        public event EventHandler<IDragDropManager.RequestFileDataArgs> FileDataRequested;

        public void CancelDrop()
        {

        }

        public void CheckForDrop()
        {
            dropHost.SendCheckForDrop();
        }

        public void DoDragDrop(ClipboardDataBase data, Guid operationId)
        {
            dropHost.SendDoDragDrop(data, operationId);
        }

        public void Start()
        {

        }

        public void Stop()
        {

        }

        public void WriteToFile(Guid messageId, byte[] data)
        {
            if (data.Length == 0)
                dropHost.SendReadReplyError(messageId);
            else
                dropHost.SendReadReply(messageId, data);
        }
    }
}
