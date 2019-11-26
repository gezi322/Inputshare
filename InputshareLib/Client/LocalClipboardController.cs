using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Clipboard;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Net;
using InputshareLib.PlatformModules.Clipboard;

namespace InputshareLib.Client
{
    internal class LocalClipboardController
    {
        public ClientDataOperation CurrentOperation { get; private set; }

        private ClipboardManagerBase clipboardMan;
        private ISClientSocket server;

        public LocalClipboardController(ClipboardManagerBase clipboardManager, ISClientSocket serverConnection)
        {
            server = serverConnection;
            clipboardMan = clipboardManager;
            clipboardMan.ClipboardContentChanged += OnLocalClipboardChange;
        }

        private void OnLocalClipboardChange(object sender, ClipboardDataBase data)
        {
            if (!server.IsConnected)
                return;

            Guid opId = Guid.NewGuid();
            CurrentOperation = new ClientDataOperation(data, opId);
            server.SendClipboardData(CurrentOperation.Data.ToBytes(), opId);
            
        }

        public void OnClipboardDataReceived(object sender, NetworkSocket.ClipboardDataReceivedArgs args)
        {
            try
            {
                ClipboardDataBase cbData = ClipboardDataBase.FromBytes(args.RawData);

                if (cbData is ClipboardVirtualFileData cbFiles)
                {
                    cbFiles.RequestPartMethod = server.RequestReadStreamAsync;
                    cbFiles.RequestTokenMethod = server.RequestFileTokenAsync;
                }

                CurrentOperation = new ClientDataOperation(cbData, args.OperationId);
                clipboardMan.SetClipboardData(cbData);
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to set clipboard data: " + ex.Message);
            }
        }
    }
}
