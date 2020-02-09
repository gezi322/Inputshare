using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Client
{
    internal class ScreenshotRequestArgs : EventArgs
    {
        internal ScreenshotRequestArgs()
        {

        }

        internal byte[] Data;
    }
}
