using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Input.Keys
{
    [Flags]
    public enum LinuxKeyMask
    {
        //TODO - these may be incorrect!
        ShiftMask = 1,
        LockMask = 2,
        ControlMask = 4,
        AltMask = 8, //MOD1
        NumLockMask = 16, //MOD2 
        ScrollLockMask = 32, //MOD3
        WindowsMask = 64, //MO4
        AltGRMask = 128, //MOD5 todo
        
        AnyModifier = 32768
    }
}
