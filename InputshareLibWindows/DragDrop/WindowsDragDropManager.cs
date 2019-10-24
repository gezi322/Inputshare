using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.DragDrop;
using InputshareLibWindows.Clipboard;
using System;
using System.Threading;
using System.Windows.Forms;
namespace InputshareLibWindows.DragDrop
{
    public class WindowsDragDropManager : IDragDropManager
    {
        private Thread dropFormThread;
        private WindowsDropTarget dropTargetWindow;

        private Thread dropSourceThread;
        private WindowsDropSource dropSourceWindow;
        public bool Running { get; private set; }
        public bool LeftMouseState { get => (Native.User32.GetAsyncKeyState(System.Windows.Forms.Keys.LButton) & 0x8000) != 0; }

        public event EventHandler<ClipboardDataBase> DataDropped;
        public event EventHandler<Guid> DragDropCancelled;
        public event EventHandler<Guid> DragDropSuccess;
        public event EventHandler<Guid> DragDropComplete;

#pragma warning disable CS0067
        public event EventHandler<IDragDropManager.RequestFileDataArgs> FileDataRequested;
#pragma warning restore CS0067

        private AutoResetEvent formLoadedEvent = new AutoResetEvent(false);

        public WindowsDragDropManager()
        {
            
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
                dropSourceWindow.DragDropComplete += DropSourceWindow_DragDropComplete;
                Application.Run(dropSourceWindow);
            });

            dropSourceThread.SetApartmentState(ApartmentState.STA);
            dropSourceThread.Start();

            if (!formLoadedEvent.WaitOne(2500))
            {
                throw new Exception("Timed out waiting for the dropsource window handle to be created");
            }
        }

        private void DropSourceWindow_DragDropComplete(object sender, Guid operationId)
        {
            DragDropComplete?.Invoke(this, operationId);
        }

        public void CancelDrop()
        {
            dropSourceWindow.CancelDrop();
        }

        private void DropSourceWindow_DragDropSuccess(object sender, Guid operationId)
        {
            if (dropTargetWindow.InputshareDataDropped)
            {
                dropTargetWindow.InputshareDataDropped = false;
                return;
            }
            
            DragDropSuccess?.Invoke(this, operationId);
        }

        private void DropSourceWindow_DragDropCancelled(object sender, Guid operationId)
        {
            DragDropCancelled?.Invoke(this, operationId);
        }

        public void DoDragDrop(ClipboardDataBase data, Guid operationId)
        {
            if(!Running)
                throw new InvalidOperationException("DragDrop manager not running");
            if (dropSourceWindow == null)
                throw new InvalidOperationException("Form not created");

            if (dropSourceWindow.Dropping)
                dropSourceWindow.CancelDrop();

            dropSourceWindow.InvokeDoDragDrop(data, operationId);
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

        public void CheckForDrop()
        {
            if(!Running)
                throw new InvalidOperationException("DragDrop manager not running");

            if (dropTargetWindow == null)
                throw new InvalidOperationException("Form not created");

            dropTargetWindow.CheckForDrop();
        }

        public void Start()
        {
            if (Running)
                throw new InvalidOperationException("DragDrop manager already running");

            InitForm();
            Running = true;
        }

        public void Stop()
        {
            if (!Running)
                throw new InvalidOperationException("DragDrop manager not running");

            dropTargetWindow.InvokeExitWindow();
            dropSourceWindow.InvokeExitWindow();
            ISLogger.Write("DragDropManager: Waiting for windows to exit");
            dropTargetWindow.WindowClosedEvent.WaitOne();
            dropSourceWindow.WindowClosedEvent.WaitOne();
            ISLogger.Write("Windows exited");

            Running = false;
            ISLogger.Write("Windows drag drop manager exited");
        }

        public void WriteToFile(Guid fileId, byte[] data)
        {
            //This method is only used for compatibility with the windows service version.
        }
    }
}
