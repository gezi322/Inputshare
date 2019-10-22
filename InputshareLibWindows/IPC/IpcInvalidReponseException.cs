using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC
{
    public class IpcInvalidReponseException : Exception
    {
        public IpcInvalidReponseException(IpcMessageType responseType, IpcMessageType expectedType) : base("Got invalid response type " + responseType + " (expected " + expectedType + ")")
        {

        }
    }
}
