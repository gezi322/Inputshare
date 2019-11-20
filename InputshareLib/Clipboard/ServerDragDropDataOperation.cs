using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Server;

namespace InputshareLib.Clipboard
{
    internal class ServerDragDropDataOperation : ServerDataOperation
    {
        internal DragDropState State { get; set; }
        internal ISServerSocket TargetClient { get; set; }

        internal ServerDragDropDataOperation(ClipboardDataBase data, ISServerSocket host) : base(data, host)
        {
        }

        internal ServerDragDropDataOperation(ClipboardDataBase data, ISServerSocket host, Guid operationId) : base(data, host, operationId)
        {
        }

        internal enum DragDropState
        {
            None,
            Dragging,
            Dropped,
            TransferingFiles,
            Complete,
            Cancelled
        }
    }
}
