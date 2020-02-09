using InputshareLib.Input;
using InputshareLib.Net;
using InputshareLib.PlatformModules.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace InputshareLib.Server.Displays
{
    /// <summary>
    /// Represents the virtual display of a client
    /// </summary>
    public abstract class DisplayBase
    {
        public event EventHandler DisplayRemoved;
        public event EventHandler<Rectangle> DisplayBoundsChanged;
        public event EventHandler<SideHitArgs> SideHit;
        public Rectangle Bounds { get; protected set; }
        public string ClientName { get; protected set; }
        public bool InputActive { get; protected set; }

        protected DisplayBase _leftDisplay;
        protected DisplayBase _rightDisplay;
        protected DisplayBase _topDisplay;
        protected DisplayBase _bottomDisplay;

        public DisplayBase(Rectangle bounds, string name)
        {
            Bounds = bounds;
            ClientName = name;
            Logger.Write($"Display {name} created ({bounds.Width}:{bounds.Height})");
        }

        protected void OnDisplayBoundsChanged(Rectangle bounds)
        {
            Logger.Write($"{ClientName}->{bounds.Width}:{bounds.Height}");
            Bounds = bounds;
            DisplayBoundsChanged?.Invoke(this, bounds);
        }

        protected void OnSideHit(Side side, int posX, int posY)
        {
            SideHit?.Invoke(this, new SideHitArgs(side, posX, posY));
        }

        /// <summary>
        /// Sets this display as the active input display
        /// </summary>
        /// <param name="newX"></param>
        /// <param name="newY"></param>
        public abstract void SetInputDisplay(int newX, int newY);

        public abstract void SetNotInputDisplay();

        /// <summary>
        /// Gets the display at the specified side and calculates the correct cursor position 
        /// </summary>
        /// <param name="side"></param>
        /// <param name="mX"></param>
        /// <param name="mY"></param>
        /// <param name="atSide"></param>
        /// <param name="newPosX"></param>
        /// <param name="newPosY"></param>
        /// <returns></returns>
        internal bool TryGetDisplayAtSide(Side side, int mX, int mY, out DisplayBase atSide, out int newPosX, out int newPosY)
        {
            atSide = null;
            newPosX = 0;
            newPosY = 0;

            switch (side)
            {
                case Side.Bottom:
                    atSide = _bottomDisplay;
                    break;
                case Side.Top:
                    atSide = _topDisplay;
                    break;
                case Side.Left:
                    atSide = _leftDisplay;
                    break;
                case Side.Right:
                    atSide = _rightDisplay;
                    break;
            }

            if (atSide == null)
                return false;

            switch (side)
            {
                case Side.Left:
                    newPosX = atSide.Bounds.Right - 2;
                    newPosY = mY;
                    break;
                case Side.Right:
                    newPosX = atSide.Bounds.Left + 2;
                    newPosY = mY;
                    break;
                case Side.Top:
                    newPosX = mX;
                    newPosY = atSide.Bounds.Bottom - 2;
                    break;
                case Side.Bottom:
                    newPosX = mX;
                    newPosY = atSide.Bounds.Top + 2;
                    break;
            }

            return true;
        }
        
        /// <summary>
        /// Sets a display to the side of this display
        /// </summary>
        /// <param name="side"></param>
        /// <param name="display"></param>
        public void SetDisplayAtEdge(Side side, DisplayBase display)
        {
            switch (side)
            {
                case Side.Bottom:
                    _bottomDisplay = display;
                    break;
                case Side.Top:
                    _topDisplay = display;
                    break;
                case Side.Left:
                    _leftDisplay = display;
                    break;
                case Side.Right:
                    _rightDisplay = display;
                    break;
            }

            Logger.Write($"{ClientName}->set side {side} to {display.ClientName}");
        }

        protected void RemoveDisplay()
        {
            Logger.Write($"Display {ClientName} removed");
            DisplayRemoved?.Invoke(this, new EventArgs());
        }

        public abstract void SendInput(ref InputData input);
    }
}
