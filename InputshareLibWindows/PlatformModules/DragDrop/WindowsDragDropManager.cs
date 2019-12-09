using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.PlatformModules.DragDrop;
using InputshareLibWindows.Clipboard;
using InputshareLibWindows.PlatformModules.Clipboard;
using System;
using System.Threading;
using System.Windows.Forms;
namespace InputshareLibWindows.PlatformModules.DragDrop
{
    public class WindowsDragDropManager : DragDropManagerBase
    {
        private Thread dropFormThread;
        private WindowsDropTarget dropTargetWindow;

        private Thread dropSourceThread;
        private WindowsDropSource dropSourceWindow;
        public override bool LeftMouseState { get => (Native.User32.GetAsyncKeyState(System.Windows.Forms.Keys.LButton) & 0x8000) != 0; protected set { return; } }

        public override event EventHandler<ClipboardDataBase> DataDropped;
        public override event EventHandler DragDropCancelled;
        public override event EventHandler DragDropSuccess;

        private AutoResetEvent formLoadedEvent = new AutoResetEvent(false);

        public WindowsDragDropManager()
        {
            
        }

        protected override void OnStart()
        {
            InitForm();
        }

        protected override void OnStop()
        {
            dropTargetWindow.InvokeExitWindow();
            dropSourceWindow.InvokeExitWindow();
            dropTargetWindow.WindowClosedEvent.WaitOne(1000);
            dropSourceWindow.WindowClosedEvent.WaitOne(1000);
        }

        private void InitForm()
        {
            dropFormThread = new Thread(() => {
                dropTargetWindow = new WindowsDropTarget(formLoadedEvent);
                dropTargetWindow.DataDropped += DropForm_DataDropped;
                Application.Run(dropTargetWindow);
            });

            dropFormThread.SetApartmentState(ApartmentState.STA);
            dropFormThread.Start();

            if (!formLoadedEvent.WaitOne(2500))
            {
                throw new Exception("Timed out waiting for the droptarget window handle to be created");
            }

            dropSourceThread = new Thread(() => {
                dropSourceWindow = new WindowsDropSource(formLoadedEvent);
                dropSourceWindow.DragDropCancelled += DropSourceWindow_DragDropCancelled;
                dropSourceWindow.DragDropSuccess += DropSourceWindow_DragDropSuccess;
                Application.Run(dropSourceWindow);
            });

            dropSourceThread.SetApartmentState(ApartmentState.STA);
            dropSourceThread.Start();

            if (!formLoadedEvent.WaitOne(2500))
            {
                throw new Exception("Timed out waiting for the dropsource window handle to be created");
            }
        }

        public override void CancelDrop()
        {
            dropSourceWindow.CancelDrop();
        }

        private void DropSourceWindow_DragDropSuccess(object sender, EventArgs _)
        {
            if (dropTargetWindow.InputshareDataDropped)
            {
                dropTargetWindow.InputshareDataDropped = false;
                return;
            }
            
            DragDropSuccess?.Invoke(this, null);
        }

        private void DropSourceWindow_DragDropCancelled(object sender, EventArgs _)
        {
            DragDropCancelled?.Invoke(this, null);
        }

        public override void DoDragDrop(ClipboardDataBase data)
        {
            if(!Running)
                throw new InvalidOperationException("DragDrop manager not running");
            if (dropSourceWindow == null)
                throw new InvalidOperationException("Form not created");

            if (dropSourceWindow.Dropping)
                dropSourceWindow.CancelDrop();

            dropSourceWindow.InvokeDoDragDrop(data);
        }

        private void DropForm_DataDropped(object sender, IDataObject data)
        {
            try
            {
                ClipboardDataBase cb = ClipboardTranslatorWindows.ConvertToGeneric(data);
                DataDropped?.Invoke(this, cb);
            }
            catch (ClipboardTranslatorWindows.ClipboardTranslationException ex)
            {
                ISLogger.Write("Failed to red clipboard data: " + ex.Message);
            }
           
        }

        public override void CheckForDrop()
        {
            ISLogger.Write("Checking for drop");

            if(!Running)
                throw new InvalidOperationException("DragDrop manager not running");

            if (dropTargetWindow == null)
                throw new InvalidOperationException("Form not created");

            dropTargetWindow.CheckForDrop();
        }
    }
}
