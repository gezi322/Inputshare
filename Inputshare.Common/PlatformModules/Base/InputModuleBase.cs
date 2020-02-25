using Inputshare.Common.Input;
using Inputshare.Common.Input.Hotkeys;
using Inputshare.Common.Input.Keys;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.Common.PlatformModules.Base
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

        public void HandleClientSwitch()
        {
            currentModifiers = 0;
        }

        protected Dictionary<Hotkey, Action> hotkeys = new Dictionary<Hotkey, Action>();
        protected KeyModifiers currentModifiers = 0;

        protected override Task OnStart()
        {
            hotkeys.Clear();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Registers a hotkey
        /// </summary>
        /// <param name="hk"></param>
        public void RegisterHotkey(Hotkey hk, Action callback)
        {
            if (IsHotkeyInUse(hk))
                throw new InvalidOperationException("Hotkey in use");

            hotkeys.Add(hk, callback);
            OnHotkeyAdded(hk);
        }

        /// <summary>
        /// Unregisters a hotkey (must be reference to object used in registerhotkey)
        /// </summary>
        /// <param name="hk"></param>
        public void RemoveHotkey(Hotkey remove)
        {
            hotkeys.Remove(remove);
            OnHotkeyRemoved(remove);
        }

        protected virtual void OnHotkeyAdded(Hotkey hk) { }
        protected virtual void OnHotkeyRemoved(Hotkey hk) { }

        protected void HandleKeyDown(WindowsVirtualKey key) {
            if (key == WindowsVirtualKey.Control || key == WindowsVirtualKey.LeftControl || key == WindowsVirtualKey.RightControl)
                currentModifiers |= KeyModifiers.Ctrl;
            else if (key == WindowsVirtualKey.Menu || key == WindowsVirtualKey.LeftMenu || key == WindowsVirtualKey.RightMenu) //menu is alt
                currentModifiers |= KeyModifiers.Alt;
            else if (key == WindowsVirtualKey.Shift || key == WindowsVirtualKey.LeftShift || key == WindowsVirtualKey.RightShift)
                currentModifiers |= KeyModifiers.Shift;
            else if (key == WindowsVirtualKey.LeftWindows || key == WindowsVirtualKey.RightWindows)
                currentModifiers |= KeyModifiers.Win;

            CheckHotkeys(key, currentModifiers);
        }

        protected void HandleKeyUp(WindowsVirtualKey key){
            if (key == WindowsVirtualKey.Control || key == WindowsVirtualKey.LeftControl || key == WindowsVirtualKey.RightControl)
                currentModifiers &= ~KeyModifiers.Ctrl;
            else if (key == WindowsVirtualKey.Menu || key == WindowsVirtualKey.LeftMenu || key == WindowsVirtualKey.RightMenu) //menu is alt
                currentModifiers &= ~KeyModifiers.Alt;
            else if (key == WindowsVirtualKey.Shift || key == WindowsVirtualKey.LeftShift || key == WindowsVirtualKey.RightShift)
                currentModifiers &= ~KeyModifiers.Shift;
            else if (key == WindowsVirtualKey.LeftWindows || key == WindowsVirtualKey.RightWindows)
                currentModifiers &= ~KeyModifiers.Win;
        }

        protected void CheckHotkeys(WindowsVirtualKey key, KeyModifiers mods)
        {
            //for each value is hotkey dictionary
            foreach(var hkDirval in hotkeys)
            {
                //if the dictionary key matches the hotkey and modifiers
                if(hkDirval.Key.Key == key && hkDirval.Key.Modifiers == mods)
                {
                    //invoke the callback method
                    hkDirval.Value();
                    return;
                }
            }
        }

        /// <summary>
        /// Returns true if a hotkey with the specified key and modifiers is already
        /// in use
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mods"></param>
        /// <returns></returns>
        internal bool IsHotkeyInUse(Hotkey hk)
        {
            foreach(var storedHk in hotkeys.Keys)
            {
                if (hk == storedHk)
                    return true;
            }
            return false;
        }

    }
}
