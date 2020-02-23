using Inputshare.Common.Clipboard;
using Inputshare.Common.Input;
using Inputshare.Common.Input.Hotkeys;
using Inputshare.Common.Input.Keys;
using Inputshare.Common.PlatformModules.Input;
using Inputshare.Common.Server.Config;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Common.Server.Display
{
    public abstract class DisplayBase
    {
        public event EventHandler<DisplayBase> DisplayRemoved;
        public event EventHandler<Side> DisplayAtSideChanged;

        public event EventHandler<Hotkey> HotkeyChanged;

        internal event EventHandler<Hotkey> HotkeyChanging;
        internal event EventHandler<ClipboardData> ClipboardChanged;
        internal event EventHandler<SideHitArgs> SideHit;

        public string DisplayName { get; }
        public Rectangle DisplayBounds { get; }
        public Hotkey Hotkey { get; internal set; }

        protected DisplayBase leftDisplay;
        protected DisplayBase rightDisplay;
        protected DisplayBase topDisplay;
        protected DisplayBase bottomDisplay;

        private ObservableDisplayList _displays;

        internal DisplayBase(ObservableDisplayList displayList, Rectangle bounds, string name)
        {
            _displays = displayList;
            DisplayName = name;
            DisplayBounds = bounds;
        }

        /// <summary>
        /// Sets the hotkey for this display. Removes the old hotkey if exists
        /// </summary>
        /// <param name="hk"></param>
        public void SetHotkey(Hotkey hk)
        {
            if (Hotkey != null)
                HotkeyChanging?.Invoke(this, Hotkey);

            HotkeyChanged?.Invoke(this, hk);
        }

        public override string ToString()
        {
            return DisplayName;
        }

        /// <summary>
        /// Sets a display to the side of this display
        /// </summary>
        /// <param name="side"></param>
        /// <param name="display"></param>
        internal void SetDisplayAtSide(Side side, DisplayBase display)
        {
            if (display == null)
                throw new ArgumentNullException(nameof(display));

            if (display == this)
                throw new ArgumentException("Cannot set X to the side of X");

            GetDisplayAtSide(side) = display;
            Logger.Verbose($"Set side {side} of {DisplayName} to {display.DisplayName}");
            DisplayAtSideChanged?.Invoke(this, side);
            SendSideChangedAsync();
        }

        /// <summary>
        /// Sets a display to the specified side of this display.
        /// If the display name is not found, the display at the specified side is removed
        /// </summary>
        /// <param name="side"></param>
        /// <param name="displayName"></param>
        public void SetDisplayAtSide(Side side, string displayName)
        {
            var target = _displays.Where(i => i.DisplayName == displayName).FirstOrDefault();

            if (target == null)
                RemoveDisplayAtSide(side);
            else
                SetDisplayAtSide(side, target);
        }

        /// <summary>
        /// Returns a reference to the variable of the display at the side of this display
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        public ref DisplayBase GetDisplayAtSide(Side side)
        {
            switch (side)
            {
                case Side.Bottom:
                    return ref bottomDisplay;
                case Side.Top:
                    return ref topDisplay;
                case Side.Left:
                    return ref leftDisplay;
                case Side.Right:
                    return ref rightDisplay;
            }
            
            //TODO - how to return a null?
            throw new Exception();
        }

        /// <summary>
        /// Removes the display at the specified side
        /// </summary>
        /// <param name="side"></param>
        public void RemoveDisplayAtSide(Side side)
        {
            Logger.Verbose($"Removing side {side} of {DisplayName}");
            GetDisplayAtSide(side) = null;
            DisplayAtSideChanged?.Invoke(this, side);
            SendSideChangedAsync();
        }

        protected void OnSideHit(Side side, int hitX, int hitY)
        {
            SideHit?.Invoke(this, new SideHitArgs(side, hitX, hitY));
        }

        protected void OnClipboardChanged(ClipboardData cbData)
        {
            ClipboardChanged?.Invoke(this, cbData);
        }

        /// <summary>
        /// Disconnects the display
        /// </summary>
        internal virtual void RemoveDisplay()
        {
            DisplayRemoved?.Invoke(this, this);
        }

        internal abstract void NotfyInputActive();
        internal abstract void NotifyClientInvactive();
        internal abstract void SendInput(ref InputData input);
        internal abstract Task SetClipboardAsync(ClipboardData cbData);

        /// <summary>
        /// Notifies the client that a display has been added to or removed from a side
        /// </summary>
        protected virtual Task SendSideChangedAsync()
        {
            foreach (Side side in (Side[])Enum.GetValues(typeof(Side)))
            {
                if(GetDisplayAtSide(side) != null)
                {
                    DisplayConfig.TrySaveClientAtSide(this, side, GetDisplayAtSide(side));
                }
            }
            
            return Task.CompletedTask;
        }
    }
}
