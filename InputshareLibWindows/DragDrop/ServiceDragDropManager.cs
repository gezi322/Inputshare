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
        private IpcHandle dropHost;
        private IpcHandle mainHost;

        public ServiceDragDropManager(IpcHandle hostMain, IpcHandle hostDragDrop)
        {
            dropHost = hostDragDrop;
            mainHost = hostMain;

            dropHost.HandleUpdated += DropHost_HandleUpdated;
            mainHost.HandleUpdated += MainHost_HandleUpdated;
            mainHost.host.LeftMouseStateUpdated += (object s, bool state) => { LeftMouseState = state; };
            dropHost.host.DataDropped += (object s, ClipboardDataBase data) => { DataDropped?.Invoke(this, data); };
            dropHost.host.DragDropCancelled += (object s, Guid id) => { DragDropCancelled?.Invoke(this, id); };
            dropHost.host.DragDropComplete += (object s, Guid id) => { DragDropComplete?.Invoke(this, id); };
            dropHost.host.DragDropSuccess += (object s, Guid id) => { DragDropSuccess?.Invoke(this, id); };
            dropHost.host.RequestedReadStream += (object s, AnonIpcHost.StreamReadRequestArgs args) => { FileDataRequested?.Invoke(this, new IDragDropManager.RequestFileDataArgs(args.MessageId, args.Token, args.FileId, args.ReadLen)); };
        }

        private void MainHost_HandleUpdated(object sender, EventArgs e)
        {
            mainHost.host.LeftMouseStateUpdated += (object s, bool state) => { LeftMouseState = state; };
        }

        private void DropHost_HandleUpdated(object sender, EventArgs e)
        {
            dropHost.host.DataDropped += (object s, ClipboardDataBase data) => { ISLogger.Write("SP DROPPED DATA"); DataDropped?.Invoke(this, data); };
            dropHost.host.DragDropCancelled += (object s, Guid id) => { DragDropCancelled?.Invoke(this, id); };
            dropHost.host.DragDropComplete += (object s, Guid id) => { DragDropComplete?.Invoke(this, id); };
            dropHost.host.DragDropSuccess += (object s, Guid id) => { DragDropSuccess?.Invoke(this, id); };
            dropHost.host.RequestedReadStream += (object s, AnonIpcHost.StreamReadRequestArgs args) => { FileDataRequested?.Invoke(this, new IDragDropManager.RequestFileDataArgs(args.MessageId, args.Token, args.FileId, args.ReadLen)); };
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
            dropHost.host.SendCheckForDrop();
        }

        public void DoDragDrop(ClipboardDataBase data, Guid operationId)
        {
            dropHost.host.SendDoDragDrop(data, operationId);
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
                dropHost.host.SendReadReplyError(messageId);
            else
                dropHost.host.SendReadReply(messageId, data);
        }
    }
}
