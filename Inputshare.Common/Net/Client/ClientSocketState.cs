using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Client
{
    internal enum ClientSocketState
    {
        Idle,
        AttemptingConnection,
        Connected
    }
}
