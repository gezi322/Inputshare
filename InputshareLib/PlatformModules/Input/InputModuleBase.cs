using InputshareLib.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace InputshareLib.PlatformModules.Input
{
    /// <summary>
    /// Base class for input modules. Used for redirecting user mouse/keyboard input#
    /// and detecting when the cursor is at the edge of a display
    /// </summary>
    public abstract class InputModuleBase : PlatformModuleBase
    {
        public abstract event EventHandler<Rectangle> DisplayBoundsUpdated;

        public abstract Rectangle VirtualDisplayBounds { get; protected set; }
        /// <summary>
        /// Fired when the cursor hits the side of the virtual screen
        /// </summary>
        public abstract event EventHandler<SideHitArgs> SideHit;

        /// <summary>
        /// Fired when A user mouse or keyboard input has being captured
        /// </summary>
        public abstract event EventHandler<InputData> InputReceived;

        /// <summary>
        /// True if the user input is currently being redirected
        /// </summary>
        public abstract bool InputRedirected { get; protected set; }

        /// <summary>
        /// Enables or disables input redirection. If input is redirected,
        /// the InputReceived event should fire each time an input is entered
        /// </summary>
        /// <param name="redirect"></param>
        public abstract void SetInputRedirected(bool redirect);
        
        /// <summary>
        /// Hides or shows the cursor
        /// </summary>
        /// <param name="hide"></param>
        public abstract void SetMouseHidden(bool hide);
    }
}
