using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Server;

namespace InputshareLib.Clipboard
{
    internal class ServerDataOperation : DataOperation
    {
        public ISServerSocket Host { get; }
        public List<Guid> RemoteAccessTokens { get; } = new List<Guid>();

        internal ServerDataOperation(ClipboardDataBase data, ISServerSocket host) : base(data)
        {
            Host = host;
        }

        internal ServerDataOperation(ClipboardDataBase data, ISServerSocket host, Guid operationId) : base(data, operationId)
        {
            OperationGuid = operationId;
            Host = host;
        }
    }
}
