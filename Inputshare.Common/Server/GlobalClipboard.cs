using Inputshare.Common.Clipboard;
using Inputshare.Common.Net.RFS;
using Inputshare.Common.Net.RFS.Client;
using Inputshare.Common.Server.Display;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Inputshare.Common.Server
{
    /// <summary>
    /// Controls the shared clipboard
    /// </summary>
    internal sealed class GlobalClipboard
    {
        private RFSController _fileController;
        private ObservableDisplayList _displays;
        private RFSFileGroup _previousClipboardFileGroup;

        internal GlobalClipboard(ObservableDisplayList clients, RFSController fileController)
        {
            _fileController = fileController;
            _displays = clients;
            clients.DisplayAdded += (object o, DisplayBase display) => OnDisplayAdded(display);
            clients.DisplayRemoved += (object o, DisplayBase display) => OnDisplayRemoved(display);
            Logger.Verbose($"Created global clipboard controller");
        }

        private void OnDisplayRemoved(DisplayBase display)
        {
            Logger.Verbose($"GlobalClipboard: Removed display {display.DisplayName}");
        }

        private void OnDisplayAdded(DisplayBase display)
        {
            Logger.Verbose($"GlobalClipboard: Added display {display.DisplayName}");
            display.ClipboardChanged += (object o, ClipboardData cbData) => OnDisplayClipboardChanged(o as DisplayBase, cbData);
        }

        private void OnDisplayClipboardChanged(DisplayBase sender, ClipboardData cbData)
        {
            Logger.Information($"GlobalClipboard: {sender.DisplayName} set clipboard");
            Logger.Debug($"Avaliable clipboard data types: {string.Join(',', cbData.AvailableTypes)}");

            //Stop hosting the previous clipboard file group
            if (_previousClipboardFileGroup != null)
                _fileController.UnHostFiles(_previousClipboardFileGroup);

            if (sender is LocalDisplay)
                OnLocalhostClipboardChanged(cbData, sender as LocalDisplay);
            else
                OnClientClipboardChanged(cbData, sender as ClientDisplay);
        }

        private void OnLocalhostClipboardChanged(ClipboardData cbData, LocalDisplay sender)
        {
            if (cbData.IsTypeAvailable(ClipboardDataType.LocalFilePaths))
            {
                //If we copied a string of file sources, we need to convert them to an RFS group
                //and host them so that clients can create streams of the files
                string[] files = cbData.GetLocalFilePaths();

                //Host the files to allow clients to access the group
                var group = _fileController.HostLocalGroup(files);

                //remote the local file paths from the clipboard
                cbData.RemoveLocalFilePaths();
                cbData.SetFileGroup(group);
                Logger.Debug($"Hosting filegroup for local files (Group ID {group.GroupId})");
            }

            BroadcastClipboard(sender, cbData);
        }

        private void OnClientClipboardChanged(ClipboardData cbData, ClientDisplay sender)
        {
            if (cbData.IsTypeAvailable(ClipboardDataType.FileGroup))
            {
                //Clients can't send files between eachother, so the rfscontroller acts as a middleman
                //by relaying requests to the client that is hosting the files
                var readableGroup = _fileController.HostRelayGroup(cbData.GetFileGroup(), sender.Socket);
                cbData.SetFileGroup(readableGroup);
                Logger.Debug($"Hosting relay filegroup for remote files (host = {sender.DisplayName}) (Group ID {readableGroup.GroupId})");
            }

            BroadcastClipboard(sender, cbData);
        }

        /// <summary>
        /// Sends the clipboard data to each client except the sender
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="cbData"></param>
        private async void BroadcastClipboard(DisplayBase sender, ClipboardData cbData)
        {
            if (cbData.IsTypeAvailable(ClipboardDataType.FileGroup))
                _previousClipboardFileGroup = cbData.GetFileGroup();

            Logger.Verbose($"Broadcasting clipboard data");
            foreach (var display in _displays.Where(i => i != sender))
            {
                await display.SetClipboardAsync(cbData);
                Logger.Verbose($"Sent clipboard data to {display.DisplayName}");
            }
        }
    }
}
