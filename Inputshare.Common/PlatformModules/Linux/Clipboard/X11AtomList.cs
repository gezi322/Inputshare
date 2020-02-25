using System;
using System.Collections.Generic;
using System.Text;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11;

namespace Inputshare.Common.PlatformModules.Linux.Clipboard
{
    internal class X11AtomList
    {
        internal IntPtr _xDisplay;

        internal X11AtomList(IntPtr xDisplay)
        {
            _xDisplay = xDisplay;
        }

        internal IntPtr Clipboard => XInternAtom(_xDisplay, "CLIPBOARD", false);
        internal IntPtr Primary => XInternAtom(_xDisplay, "PRIMARY", false);
        internal IntPtr Targets => XInternAtom(_xDisplay, "TARGETS", false);
        internal IntPtr Utf8String => XInternAtom(_xDisplay, "UTF8_STRING", false);
        internal IntPtr Text => XInternAtom(_xDisplay, "TEXT", false);
        internal IntPtr String => XInternAtom(_xDisplay, "STRING", false);
        internal IntPtr ImagePng => XInternAtom(_xDisplay, "image/png", false);
        internal IntPtr UriListReturn => XInternAtom(_xDisplay, "cbReturnPropFile", false);
        internal IntPtr TextReturn => XInternAtom(_xDisplay, "cbReturnPropText", false);
        internal IntPtr ImageReturn => XInternAtom(_xDisplay, "cbReturnPropImage", false);
        internal IntPtr INCR => XInternAtom(_xDisplay, "INCR", false);
        internal IntPtr UriList => XInternAtom(_xDisplay, "text/uri-list", false);
    }
}
