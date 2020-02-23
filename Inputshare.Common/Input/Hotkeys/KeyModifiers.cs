using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Input.Hotkeys
{
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Ctrl = 2,
        Shift = 4,
        Win = 8
    }
}
