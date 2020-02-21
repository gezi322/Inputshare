using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Input
{
    /// <summary>
    /// Repesents an platform independant input type
    /// </summary>
    public enum InputCode : byte
    {
        Unkown = 0,
        MouseMoveRelative,
        MouseMoveAbsolute,
        Mouse1Down,
        Mouse1Up,
        Mouse2Down,
        Mouse2Up,
        MouseMDown,
        MouseMUp,
        MouseXDown,
        MouseXUp,
        MouseYScroll,
        KeyDownScan,
        keyUpScan,
        KeyDownVKey,
        KeyUpVKey
    }
}
