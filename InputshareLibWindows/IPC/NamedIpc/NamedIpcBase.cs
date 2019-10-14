using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Pipes;
using InputshareLib;

namespace InputshareLibWindows.IPC.NamedIpc
{
    public class NamedIpcBase
    {
        private PipeStream ioStream;
        private byte[] readBuffer = new byte[1024];


        public NamedIpcBase(NamedIpcMode mode)
        {
            if (mode == NamedIpcMode.Client)
                StartClient();
            else
                StartServer();
        }

        private void StartClient()
        {
            ioStream = new NamedPipeClientStream("*", "InputsharePipeHost", PipeDirection.InOut);
        }

        private void StartServer()
        {
            NamedPipeServerStream host = new NamedPipeServerStream("InputsharePipeHost", PipeDirection.InOut);
            ioStream = host;
            host.BeginWaitForConnection(WaitForConnectionCallback, host);
        }

        private void WaitForConnectionCallback(IAsyncResult ar)
        {
            try
            {
                NamedPipeServerStream host = (NamedPipeServerStream)ar.AsyncState;
                ISLogger.Write("NamedIpcBase: Client connected");
            }catch(Exception ex)
            {
                ISLogger.Write("NamedIpcBase: Failed to wait for connection: " + ex.Message);
            }
        }

        public enum NamedIpcMode
        {
            Client,
            Server
        }
    }
}
