using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.PlatformModules.DragDrop;
using InputshareLibWindows.IPC.AnonIpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.PlatformModules.DragDrop
{
    public class ServiceDragDropManager : DragDropManagerBase
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
            dropHost.host.DragDropCancelled += (object s, Guid id) => { DragDropCancelled?.Invoke(this, null); };;
            dropHost.host.DragDropSuccess += (object s, Guid id) => { DragDropSuccess?.Invoke(this, null); };
            dropHost.host.RequestedReadStream += (object s, AnonIpcHost.StreamReadRequestArgs args) => { FileDataRequested?.Invoke(this, new DragDropManagerBase.RequestFileDataArgs(args.MessageId, args.Token, args.FileId, args.ReadLen)); };
        }

        protected override void OnStart()
        {

        }

        protected override void OnStop()
        {

        }

        private void MainHost_HandleUpdated(object sender, EventArgs e)
        {
            mainHost.host.LeftMouseStateUpdated += (object s, bool state) => { LeftMouseState = state; };
        }

        private void DropHost_HandleUpdated(object sender, EventArgs e)
        {
            dropHost.host.DataDropped += (object s, ClipboardDataBase data) => { ISLogger.Write("SP DROPPED DATA"); DataDropped?.Invoke(this, data); };
            dropHost.host.DragDropCancelled += (object s, Guid id) => { DragDropCancelled?.Invoke(this, null); };
            dropHost.host.DragDropSuccess += (object s, Guid id) => { DragDropSuccess?.Invoke(this, null); };
            dropHost.host.RequestedReadStream += (object s, AnonIpcHost.StreamReadRequestArgs args) => { FileDataRequested?.Invoke(this, new DragDropManagerBase.RequestFileDataArgs(args.MessageId, args.Token, args.FileId, args.ReadLen)); };
        }
        public override bool LeftMouseState { get; protected set; } = false;

        public override event EventHandler DragDropCancelled;
        public override event EventHandler DragDropSuccess;
        public override event EventHandler<ClipboardDataBase> DataDropped;
        public override event EventHandler<DragDropManagerBase.RequestFileDataArgs> FileDataRequested;

        public override void CancelDrop()
        {

        }

        public override void CheckForDrop()
        {
            dropHost.host.SendCheckForDrop();
        }

        public override void DoDragDrop(ClipboardDataBase data, Guid operationId)
        {
            dropHost.host.SendDoDragDrop(data, operationId);
        }

        public override  void WriteToFile(Guid messageId, byte[] data)
        {
            if (data.Length == 0)
                dropHost.host.SendReadReplyError(messageId);
            else
                dropHost.host.SendReadReply(messageId, data);
        }
    }
}
