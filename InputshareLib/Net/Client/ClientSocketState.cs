using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Client
{
    internal enum ClientSocketState
    {
        Idle,
        AttemptingConnection,
        Connected
    }
}
