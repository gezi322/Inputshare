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
        public override bool LeftMouseState { get; protected set; } = false;

        public override event EventHandler DragDropCancelled;
        public override event EventHandler DragDropSuccess;
        public override event EventHandler<ClipboardDataBase> DataDropped;

        private Dictionary<Guid, CallbackHolder> callbacks = new Dictionary<Guid, CallbackHolder>();

        private IpcHandle clipboardHost;

        public ServiceDragDropManager(IpcHandle hostDragDrop)
        {
            clipboardHost = hostDragDrop;
            clipboardHost.HandleUpdated += DropHost_HandleUpdated;
            clipboardHost.host.LeftMouseStateUpdated += (object s, bool state) => { LeftMouseState = state; };
            clipboardHost.host.DataDropped += (object s, ClipboardDataBase data) => { DataDropped?.Invoke(this, data); };
            clipboardHost.host.DragDropCancelled += (object s, EventArgs _) => { DragDropCancelled?.Invoke(this, null); };;
            clipboardHost.host.DragDropSuccess += (object s, EventArgs _) => { DragDropSuccess?.Invoke(this, null); };
            clipboardHost.host.RequestedReadStream += Host_RequestedReadStream;
            clipboardHost.host.RequestedFileToken += Host_RequestedFileToken;
        }

        protected override void OnStart()
        {

        }

        protected override void OnStop()
        {

        }

        private void DropHost_HandleUpdated(object sender, EventArgs e)
        {
            clipboardHost.host.LeftMouseStateUpdated += (object s, bool state) => { LeftMouseState = state; };
            clipboardHost.host.DataDropped += (object s, ClipboardDataBase data) => { ISLogger.Write("SP DROPPED DATA"); DataDropped?.Invoke(this, data); };
            clipboardHost.host.DragDropCancelled += (object s, EventArgs _) => { DragDropCancelled?.Invoke(this, null); };
            clipboardHost.host.DragDropSuccess += (object s, EventArgs _) => { DragDropSuccess?.Invoke(this, null); };
            clipboardHost.host.RequestedReadStream += Host_RequestedReadStream;
            clipboardHost.host.RequestedFileToken += Host_RequestedFileToken;
        }

        private async void Host_RequestedReadStream(object sender, AnonIpcHost.StreamReadRequestArgs args)
        {
            try
            {
                if (!callbacks.TryGetValue(args.Token, out CallbackHolder cbHolder))
                    return;

                byte[] data = await cbHolder.RequestPart(args.Token, args.FileId, args.ReadLen);
                clipboardHost.host.SendReadReply(args.MessageId, data);
               
            }
            catch(Exception ex)
            {
                ISLogger.Write("ServiceDragDropManager: Failed to send file data: " + ex.Message);
                ISLogger.Write(ex.StackTrace);
            }
        }

        private async void Host_RequestedFileToken(object sender, AnonIpcHost.FileTokenRequestArgs args)
        {
            try
            {
                if (!callbacks.TryGetValue(args.Operation, out CallbackHolder cbHolder))
                    return;

                Guid token = await cbHolder.RequestToken(args.Operation);
                callbacks.Add(token, cbHolder);
                clipboardHost.host.SendFileTokenResponse(args.MessageId, token);
                ISLogger.Write("Sent file token response " + token);
            }catch(Exception ex)
            {
                ISLogger.Write("ServiceDragDropManager: Failed to send file token: " + ex.Message);
                ISLogger.Write(ex.StackTrace);
            }
        }

        public override void CancelDrop()
        {

        }

        public override void CheckForDrop()
        {
            clipboardHost.host.SendCheckForDrop();
        }

        public override void DoDragDrop(ClipboardDataBase data)
        {
            if(data is ClipboardVirtualFileData cbFiles)
            {
                callbacks.Add(cbFiles.OperationId, new CallbackHolder(cbFiles.RequestPartMethod, cbFiles.RequestTokenMethod));
            }

            clipboardHost.host.SendDoDragDrop(data, data.OperationId);
        }

        private class CallbackHolder
        {
            public CallbackHolder(ClipboardVirtualFileData.RequestPartDelegate requestPart, ClipboardVirtualFileData.RequestTokenDelegate requestToken)
            {
                RequestPart = requestPart;
                RequestToken = requestToken;
            }

            public ClipboardVirtualFileData.RequestPartDelegate RequestPart { get; }
            public ClipboardVirtualFileData.RequestTokenDelegate RequestToken { get; }
        }
    }
}
