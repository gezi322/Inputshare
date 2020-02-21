using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Client
{
    internal class ScreenshotRequestArgs : EventArgs
    {
        internal ScreenshotRequestArgs()
        {

        }

        internal byte[] Data;
    }
}
