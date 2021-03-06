﻿using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Input
{
    public enum LinuxKeyCode
    {
        Back = 22,
        Tab = 23,
        Return = 36,
        Shift = 50, //Shift is mapped to leftshift
        Control = 37,
        Menu = 64,
        Pause = 110,
        CapsLock = 66,

        #region IME 
        Kana = 90,
        Hanguel = 122,
        Hanja = 123,
        #endregion
        Escape = 9,
        Space = 65,
        Prior = 112,
        Next = 117,
        End = 115,
        Home = 110,
        Left = 113,
        Up = 111,
        Right = 114,
        Down = 116,
        Snapshot = 107,
        Insert = 118,
        Delete = 119,
        D0 = 19,
        D1 = 10,
        D2 = 11,
        D3 = 12,
        D4 = 13,
        D5 = 14,
        D6 = 15,
        D7 = 16,
        D8 = 17,
        D9 = 18,
        A = 38,
        B = 56,
        C = 54,
        D = 40,
        E = 26,
        F = 41,
        G = 42,
        H = 43,
        I = 31,
        J = 44,
        K = 45,
        L = 46,
        M = 58,
        N = 57,
        O = 32,
        P = 33,
        Q = 24,
        R = 27,
        S = 39,
        T = 28,
        U = 30,
        V = 55,
        W = 25,
        X = 53,
        Y = 29,
        Z = 52,
        LeftWindows = 133,
        RightWindows = 134,
        Application = 135,
        Sleep = 150,
        Numpad0 = 90,
        Numpad1 = 87,
        Numpad2 = 88,
        Numpad3 = 89,
        Numpad4 = 83,
        Numpad5 = 84,
        Numpad6 = 85,
        Numpad7 = 79,
        Numpad8 = 80,
        Numpad9 = 81,
        Multiply = 63,
        Add = 86, // todo
        Subtract = 20,
        Decimal = 60,
        Divide = 106,
        F1 = 67,
        F2 = 68,
        F3 = 69,
        F4 = 70,
        F5 = 71,
        F6 = 72,
        F7 = 73,
        F8 = 74,
        F9 = 75,
        F10 = 76,
        F11 = 95,
        F12 = 96,
        F13 = 183,
        F14 = 184,
        F15 = 185,
        F16 = 186,
        F17 = 187,
        F18 = 188,
        F19 = 189,
        F20 = 190,
        F21 = 191,
        F22 = 192,
        F23 = 193,
        F24 = 194,
        NumLock = 77,
        ScrollLock = 78,
        LeftShift = 50,
        RightShift = 62,
        LeftControl = 37,
        RightControl = 109,
        LeftMenu = 64,
        RightMenu = 0,
        BrowserBack = 166,
        BrowserForward = 167,
        BrowserRefresh = 0,
        BrowserStop = 128,
        BrowserSearch = 136,
        BrowserFavorites = 156,
        BrowserHome = 172,
        VolumeMute = 121,
        VolumeDown = 122,
        VolumeUp = 123,
        MediaNextTrack = 171,
        MediaPrevTrack = 173,
        MediaStop = 174,
        MediaPlayPause = 172,
        LaunchMail = 155,
        LaunchMediaSelect = 0,
        LaunchApplication1 = 156,
        LaunchApplication2 = 0,

        //These keys may be incorrect for non-UK keyboard layouts
        OEM1 = 47, //semi colon
        OEMPlus = 21,
        OEMComma = 59,
        OEMMinus = 20,
        OEMPeriod = 60,
        OEM2 = 61, //forward slash
        OEM3 = 48, //Apostrophe
        OEM4 = 34, //Left bracket
        OEM5 = 94, //back slash
        OEM6 = 35, //right brace
        OEM7 = 51, //hash tag (AKA numbersign)
        OEM8 = 49, //Grave key
        Play = 207,
        Zoom = 0,
        OEMClear = 0,

        //These values can't be mapped directly to windows keys
        KPRETURN = 104,
        KPDELETE = 91,
        KPSUBTRACT = 82,
        KPPLUS = 86,
    }
}
