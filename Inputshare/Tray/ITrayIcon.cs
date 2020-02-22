using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Tray
{
    internal interface ITrayIcon : IDisposable
    {
        event EventHandler TrayIconClicked;
        event EventHandler TrayIconDoubleClicked;
    }
}
