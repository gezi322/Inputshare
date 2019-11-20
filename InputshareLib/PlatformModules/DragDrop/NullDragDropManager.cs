using InputshareLib.Clipboard.DataTypes;
using System;

namespace InputshareLib.PlatformModules.DragDrop
{
    public class NullDragDropManager : DragDropManagerBase
    {
        public override bool LeftMouseState { get => false; protected set { return; } }

#pragma warning disable CS0067
        public override event EventHandler DragDropCancelled;
        public override event EventHandler DragDropSuccess;
        public override event EventHandler<ClipboardDataBase> DataDropped;
        public override event EventHandler<DragDropManagerBase.RequestFileDataArgs> FileDataRequested;
#pragma warning restore CS0067
        public override void CancelDrop()
        {
        }

        public override void CheckForDrop()
        {
        }

        public override void DoDragDrop(ClipboardDataBase data)
        {

        }

        public override void WriteToFile(Guid fileId, byte[] data)
        {

        }

        protected override void OnStart()
        {

        }

        protected override void OnStop()
        {

        }
    }
}
