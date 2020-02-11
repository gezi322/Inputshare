using InputshareLib.Input;
using InputshareLib.PlatformModules.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Server.Display
{
    public abstract class DisplayBase
    {
        public event EventHandler<DisplayBase> DisplayRemoved;
        internal event EventHandler<SideHitArgs> SideHit;

        public string DisplayName { get; }
        public Rectangle DisplayBounds { get; }

        protected DisplayBase leftDisplay;
        protected DisplayBase rightDisplay;
        protected DisplayBase topDisplay;
        protected DisplayBase bottomDisplay;

        internal DisplayBase(Rectangle bounds, string name)
        {
            DisplayName = name;
            DisplayBounds = bounds;
            Logger.Write($"Created display {name} ({bounds.Width}:{bounds.Height})");
        }

        /// <summary>
        /// Sets a display to the side of this display
        /// </summary>
        /// <param name="side"></param>
        /// <param name="display"></param>
        public void SetDisplayAtSide(Side side, DisplayBase display)
        {
            if (display == null)
                throw new ArgumentNullException(nameof(display));

            GetDisplayAtSide(side) = display;
            Logger.Write($"Set side {side} of {DisplayName} to {display.DisplayName}");
            SendSideChangedAsync();
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

        public void RemoveDisplayAtSide(Side side)
        {
            Logger.Write($"Removing side {side} of {DisplayName}");
            GetDisplayAtSide(side) = null;
            SendSideChangedAsync();
        }

        protected void OnSideHit(Side side, int hitX, int hitY)
        {
            SideHit?.Invoke(this, new SideHitArgs(side, hitX, hitY));
        }

        /// <summary>
        /// Disconnects the display
        /// </summary>
        internal virtual void RemoveDisplay()
        {
            DisplayRemoved?.Invoke(this, this);
        }

        internal abstract Task NotfyInputActiveAsync();
        internal abstract Task NotifyClientInvactiveAsync();
        internal abstract void SendInput(ref InputData input);

        /// <summary>
        /// Notifies the client that a display has been added to or removed from a side
        /// </summary>
        protected abstract Task SendSideChangedAsync();
    }
}
