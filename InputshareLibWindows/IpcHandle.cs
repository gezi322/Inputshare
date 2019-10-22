using InputshareLibWindows.IPC;
using InputshareLibWindows.IPC.AnonIpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows
{

    /// <summary>
    /// Contains a pointer to an AnonIpcClient that can be changed at any time
    /// by the service.
    /// </summary>
    public class IpcHandle
    {
        public event EventHandler HandleUpdated;

        public void NotifyHandleUpdate()
        {
            HandleUpdated?.Invoke(this, null);
        }

        public AnonIpcHost host { get; set; }
    }
}
