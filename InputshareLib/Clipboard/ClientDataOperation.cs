using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Clipboard.DataTypes;

namespace InputshareLib.Clipboard
{
    internal class ClientDataOperation : DataOperation
    {
        public List<Guid> RemoteFileAccessTokens { get; } = new List<Guid>();
        internal ClientDataOperation(ClipboardDataBase data) : base(data)
        {
        }

        internal ClientDataOperation(ClipboardDataBase data, Guid operationId) : base(data, operationId)
        {
        }
    }
}
