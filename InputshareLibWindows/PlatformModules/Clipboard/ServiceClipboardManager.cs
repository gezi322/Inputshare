using InputshareLib;
using InputshareLib.Clipboard;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.PlatformModules.Clipboard;
using InputshareLibWindows.IPC.AnonIpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.Clipboard
{
    public class ServiceClipboardManager : ClipboardManagerBase
    {
        private IpcHandle host;

        private Dictionary<Guid, CallbackHolder> callbacks = new Dictionary<Guid, CallbackHolder>();

        public ServiceClipboardManager(IpcHandle clipboardHost)
        {
            host = clipboardHost;
            host.HandleUpdated += Host_HandleUpdated;
            host.host.ClipboardDataReceived += Host_ClipboardDataReceived;
            host.host.RequestedFileToken += Host_RequestedFileToken;
            host.host.RequestedReadStream += Host_RequestedReadStream;
        }

        private async void Host_RequestedReadStream(object sender, AnonIpcHost.StreamReadRequestArgs args)
        {
            try
            {
                if (!callbacks.TryGetValue(args.Token, out CallbackHolder cbHolder))
                    return;

                byte[] data = await cbHolder.RequestPart(args.Token, args.FileId, args.ReadLen);
                host.host.SendReadReply(args.MessageId, data);

            }
            catch (Exception ex)
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
                host.host.SendFileTokenResponse(args.MessageId, token);
                ISLogger.Write("Sent file token response " + token);
            }
            catch (Exception ex)
            {
                ISLogger.Write("ServiceDragDropManager: Failed to send file token: " + ex.Message);
                ISLogger.Write(ex.StackTrace);
            }
        }

        protected override void OnStart()
        {

        }

        protected override void OnStop()
        {

        }

        private void Host_HandleUpdated(object sender, EventArgs e)
        {
            host.host.ClipboardDataReceived += Host_ClipboardDataReceived;
        }

        private void Host_ClipboardDataReceived(object sender, ClipboardDataBase data)
        {
            OnClipboardDataChanged(data);
        }

        public override void SetClipboardData(ClipboardDataBase data) {
            if (data is ClipboardVirtualFileData cbFiles)
            {
                callbacks.Add(cbFiles.OperationId, new CallbackHolder(cbFiles.RequestPartMethod, cbFiles.RequestTokenMethod));
            }

            ISLogger.Write("Set clipboard data " + data.OperationId);

            host.host.SendClipboardData(data);
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
