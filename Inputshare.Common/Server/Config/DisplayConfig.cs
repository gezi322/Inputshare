using Inputshare.Common.Input.Hotkeys;
using Inputshare.Common.Server.Display;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Inputshare.Common.Server.Config
{
    /// <summary>
    /// Writes and reads properties for a display to/from the configuration file
    /// </summary>
    internal static class DisplayConfig
    {
        internal static bool TryGetClientAtSide(DisplayBase display, Side side, out string clientName)
        {
            string prop = "Server.Display." + display.DisplayName + "." + side.ToString();
            return DllConfig.TryReadProperty(prop, out clientName);
        }

        internal static bool TrySaveClientAtSide(DisplayBase display, Side side, DisplayBase sideDisplay)
        {
            return DllConfig.TryWrite("Server.Display." + display.DisplayName + "." + side.ToString(), sideDisplay.DisplayName);
        }

        internal static bool TryGetClientHotkey(DisplayBase display, out Hotkey hk)
        {
            hk = null;

            if (DllConfig.TryReadProperty("Server.Display." + display.DisplayName + ".Hotkey", out string hkStr))
                if (Hotkey.TryReadFromSettingsString(hkStr, out hk))
                    return true;

            return false;
        }

        internal static bool TrySaveClientHotkey(DisplayBase display, Hotkey hk)
        {
            return DllConfig.TryWrite("Server.Display." + display.DisplayName + ".Hotkey", hk.ToSettingsString());
        }
    }
}
