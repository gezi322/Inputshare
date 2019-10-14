using InputshareLib;
using InputshareLib.Clipboard;
using InputshareLib.Clipboard.DataTypes;
using InputshareLibWindows.IPC.AnonIpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.Clipboard
{
    public class ServiceClipboardManager : ClipboardManagerBase
    {
        private AnonIpcHost host;

        public ServiceClipboardManager(AnonIpcHost mainHost)
        {
            host = mainHost;
            host.ClipboardDataReceived += Host_ClipboardDataReceived;
        }

        private void Host_ClipboardDataReceived(object sender, ClipboardDataBase data)
        {
            OnClipboardDataChanged(data);
        }

        public override void SetClipboardData(ClipboardDataBase data) {
            host.SendClipboardData(data);
        }

        public override void Start()
        {
        }

        public override void Stop()
        {
        }
    }
}
