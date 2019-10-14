using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Clipboard.DataTypes;

namespace InputshareLib.DragDrop
{
    public class NullDragDropManager : IDragDropManager
    {
        public bool Running { get; private set; }

        public bool LeftMouseState { get => false; }

        public event EventHandler<Guid> DragDropCancelled;
        public event EventHandler<Guid> DragDropSuccess;
        public event EventHandler<Guid> DragDropComplete;
        public event EventHandler<ClipboardDataBase> DataDropped;
        public event EventHandler<IDragDropManager.RequestFileDataArgs> FileDataRequested;

        public void CancelDrop()
        {
        }

        public void CheckForDrop()
        {
        }

        public void DoDragDrop(ClipboardDataBase data, Guid operationId)
        {

        }

        public void Start()
        {
            Running = true;
        }

        public void Stop()
        {
            Running = false;
        }

        public void WriteToFile(Guid fileId, byte[] data)
        {

        }
    }
}
