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

        internal bool ClipboardEnabled { get; set; } = true;

        private ClipboardManagerBase clipboardMan;
        private ISClientSocket server;

        public LocalClipboardController(ClipboardManagerBase clipboardManager, ISClientSocket serverConnection)
        {
            server = serverConnection;
            server.ClipboardDataReceived += OnClipboardDataReceived;
            clipboardMan = clipboardManager;
            clipboardMan.ClipboardContentChanged += OnLocalClipboardChange;
        }

        private void OnLocalClipboardChange(object sender, ClipboardDataBase data)
        {
            if (!server.IsConnected || !ClipboardEnabled)
                return;


            Guid opId = Guid.NewGuid();
            CurrentOperation = new ClientDataOperation(data, opId);
            server.SendClipboardData(CurrentOperation.Data.ToBytes(), opId);
            
        }

        public void OnClipboardDataReceived(object sender, NetworkSocket.ClipboardDataReceivedArgs args)
        {
            if (!ClipboardEnabled)
                return;

            try
            {
                ClipboardDataBase cbData = ClipboardDataBase.FromBytes(args.RawData);
                cbData.OperationId = args.OperationId;

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
