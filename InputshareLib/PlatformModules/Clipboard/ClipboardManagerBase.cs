using InputshareLib.Clipboard.DataTypes;
using System;

namespace InputshareLib.PlatformModules.Clipboard
{

    /// <summary>
    /// Manages the windows clipboard.
    /// Sets clipboard data and monitors for changes.
    /// </summary>
    public abstract class ClipboardManagerBase : PlatformModuleBase
    {
        public event EventHandler<ClipboardDataBase> ClipboardContentChanged;
        public abstract void SetClipboardData(ClipboardDataBase data);

        protected void OnClipboardDataChanged(ClipboardDataBase data)
        {
            ClipboardContentChanged?.Invoke(this, data);
        }
    }
}
