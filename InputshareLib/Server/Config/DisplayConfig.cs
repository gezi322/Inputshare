using InputshareLib.Server.Display;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace InputshareLib.Server.Config
{
    internal static class DisplayConfig
    {
        internal static bool TryGetClientAtSide(DisplayBase display, Side side, out string clientName)
        {
            string prop = display.DisplayName + "." + side.ToString();
            return DllConfig.TryReadProperty(prop, out clientName);
        }

        internal static bool TrySaveClientAtSide(DisplayBase display, Side side, DisplayBase sideDisplay)
        {
            return DllConfig.TryWrite(display.DisplayName + "." + side.ToString(), sideDisplay.DisplayName);
        }
    }
}
