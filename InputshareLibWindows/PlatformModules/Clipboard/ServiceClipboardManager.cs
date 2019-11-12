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

        public ServiceClipboardManager(IpcHandle mainHost)
        {
            host = mainHost;
            host.HandleUpdated += Host_HandleUpdated;
            host.host.ClipboardDataReceived += Host_ClipboardDataReceived;
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
            host.host.SendClipboardData(data);
        }

        
    }
}
