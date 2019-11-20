using InputshareLib.Clipboard.DataTypes;
using System;

namespace InputshareLib.PlatformModules.DragDrop
{
    public abstract class DragDropManagerBase : PlatformModuleBase
    {
        public abstract event EventHandler DragDropCancelled;
        public abstract event EventHandler DragDropSuccess;
        public abstract event EventHandler<ClipboardDataBase> DataDropped;
        public abstract event EventHandler<RequestFileDataArgs> FileDataRequested;

        public abstract bool LeftMouseState { get; protected set; }

        public abstract void CancelDrop();
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
