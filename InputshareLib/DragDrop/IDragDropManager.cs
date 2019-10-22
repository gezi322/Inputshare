using InputshareLib.Clipboard.DataTypes;
using System;

namespace InputshareLib.DragDrop
{
    public interface IDragDropManager
    {
        public event EventHandler<Guid> DragDropCancelled;
        public event EventHandler<Guid> DragDropSuccess;
        public event EventHandler<Guid> DragDropComplete;
        public event EventHandler<ClipboardDataBase> DataDropped;
        public event EventHandler<RequestFileDataArgs> FileDataRequested;

        public abstract bool Running { get; }
        public abstract bool LeftMouseState { get; }

        public abstract void CancelDrop();
        public abstract void Start();
        public abstract void Stop();
        public abstract void CheckForDrop();
        public abstract void DoDragDrop(ClipboardDataBase data, Guid operationId);
        public abstract void WriteToFile(Guid fileId, byte[] data);


        public class RequestFileDataArgs
        {
            public RequestFileDataArgs(Guid messageId, Guid token, Guid fileId, int readLen)
            {
                MessageId = messageId;
                Token = token;
                FileId = fileId;
                ReadLen = readLen;
            }

            public Guid MessageId { get; }
            public Guid Token { get; }
            public Guid FileId { get; }
            public int ReadLen { get; }
        }
    }
}
