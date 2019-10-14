using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;

namespace InputshareLibWindows.IPC.NamedIpc
{
    public class NamedPipeBase
    {
        private PipeStream rwPipe;

        public NamedPipeBase(NamedPipeServerStream server)
        {

        }

        public NamedPipeBase(NamedPipeClientStream client)
        {

        }
    }
}
