using InputshareLib.Clipboard;
using InputshareLib.Net.RFS;
using InputshareLib.Net.RFS.Client;
using InputshareLib.Server.Display;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace InputshareLib.Server
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
        }

        private void OnDisplayRemoved(DisplayBase display)
        {
            Logger.Write("GlobalClipboard: removed " + display.DisplayName);

        }

        private void OnDisplayAdded(DisplayBase display)
        {
            Logger.Write("GlobalClipboard: added " + display.DisplayName);
            display.ClipboardChanged += (object o, ClipboardData cbData) => OnDisplayClipboardChanged(o as DisplayBase, cbData);
        }

        private async void OnDisplayClipboardChanged(DisplayBase sender, ClipboardData cbData)
        {
            Logger.Write($"GlobalClipboard -> {sender.DisplayName} set clipboard");

            //Stop hosting the previous clipboard file group
            if (_previousClipboardFileGroup != null)
                _fileController.UnHostFiles(_previousClipboardFileGroup);

            if (sender is LocalDisplay && cbData.IsTypeAvailable(ClipboardDataType.HostFileGroup))
            {
                //If localhost copied a list of file sources, convert them into an RFS file group
                //that a client can read from
                string[] files = cbData.GetLocalFiles();
                var group = _fileController.HostLocalGroup(files);
                cbData.SetRemoteFiles(group);
                _previousClipboardFileGroup = group;
            }
            else if (!(sender is LocalDisplay) && cbData.IsTypeAvailable(ClipboardDataType.HostFileGroup))
            {
                //If another client set the clipboard files, convert the given filegroup into a client filegroup
                //that the server, and other clients can read from the remote file group
                var group = cbData.GetRemoteFiles();
                var clientGroup = RFSClientFileGroup.FromGroup(group, (sender as ClientDisplay).Socket);
                _fileController.HostRemoteGroup(clientGroup);
                cbData.SetRemoteFiles(clientGroup);
                _previousClipboardFileGroup = group;
            }

            foreach (var display in _displays.Where(i => i != sender))
            {
                await display.SetClipboardAsync(cbData);
            }
        }
    }
}
