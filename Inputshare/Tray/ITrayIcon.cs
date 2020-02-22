using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Tray
{
    internal interface ITrayIcon
    {
        event EventHandler TrayIconClicked;
        void MinimizeToTray();
    }
}
