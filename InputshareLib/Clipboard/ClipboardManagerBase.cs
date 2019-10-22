using InputshareLib.Clipboard.DataTypes;
using System;

namespace InputshareLib.Clipboard
{

    /// <summary>
    /// Manages the windows clipboard.
    /// Sets clipboard data and monitors for changes.
    /// </summary>
    public abstract class ClipboardManagerBase
    {
        public event EventHandler<ClipboardDataBase> ClipboardContentChanged;

        public bool Running { get; protected set; }

        public abstract void Start();
        public abstract void Stop();

        public abstract void SetClipboardData(ClipboardDataBase data);

        protected void OnClipboardDataChanged(ClipboardDataBase data)
        {
            ClipboardContentChanged?.Invoke(this, data);
        }
    }
}
