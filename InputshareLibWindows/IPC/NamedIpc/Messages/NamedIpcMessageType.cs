using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.NamedIpc.Messages
{
    public enum NamedIpcMessageType
    {
        Unknown = 0,
        HostOK,
        ClientOK,
        StateRequest,
        StateResponse,
        Connect,
        Disconnect,
        EnableAutoReconnect,
        DisableAutoReconnect,
    }
}
