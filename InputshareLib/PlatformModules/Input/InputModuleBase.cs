using InputshareLib.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.PlatformModules.Input
{
    /// <summary>
    /// Base class for input modules. Used for redirecting user mouse/keyboard input
    /// </summary>
    public abstract class InputModuleBase : PlatformModuleBase
    {
        /// <summary>
        /// Fired when the cursor hits the side of the virtual screen
        /// </summary>
        public abstract event EventHandler<Side> SideHit;

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
    }
}
