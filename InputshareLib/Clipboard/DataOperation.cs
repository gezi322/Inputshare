using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Clipboard.DataTypes;

namespace InputshareLib.Clipboard
{
    internal class DataOperation
    {
        internal DataOperation(ClipboardDataBase data)
        {
            Data = data;
            OperationGuid = Guid.NewGuid();
            data.OperationId = OperationGuid;
        }

        internal DataOperation(ClipboardDataBase data, Guid operationId)
        {
            Data = data;
            OperationGuid = operationId;
            data.OperationId = operationId;
        }

        public Guid OperationGuid { get; protected set; }
        public ClipboardDataBase Data { get; protected set; }
        public DateTime StartTime { get; } = DateTime.Now;
        public Guid LocalAccessToken { get; set; }
    }
}
