using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Input;
using InputshareLibWindows.Clipboard;
using InputshareLibWindows.Native;
using InputshareLibWindows.Output;
using InputshareLibWindows.PlatformModules.Clipboard;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using static InputshareLibWindows.Native.Ole32;
using static InputshareLibWindows.Native.User32;
using DragDropEffects = System.Windows.Forms.DragDropEffects;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using InputshareLib.Input.Keys;

namespace InputshareLibWindows.PlatformModules.DragDrop
{
    class WindowsDropSource : Form, IDropSource
    {
        public event EventHandler DragDropCancelled;
        public event EventHandler DragDropSuccess;

        private AutoResetEvent HandleCreatedEvent;
        public readonly AutoResetEvent WindowClosedEvent = new AutoResetEvent(false);

        public bool Dropping { get; private set; }

        private WindowsOutputManager outMan = new WindowsOutputManager();

        private bool dropSourceAllowDrop = false;
        private bool reportedSuccess = false;

        private Queue<IDataObject> dropQueue = new Queue<IDataObject>();
        private bool cancelDrop = false;
        private bool registeredDrop = false;

        public WindowsDropSource(AutoResetEvent handleCreatedEvent)
        {
            HandleCreatedEvent = handleCreatedEvent;
            this.Load += WindowsDragSource_Load;
            this.FormClosed += WindowsDropSource_FormClosed;
        }

        private void WindowsDropSource_FormClosed(object sender, FormClosedEventArgs e)
        {
            WindowClosedEvent?.Set();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0201) //left mouse down
            {
                if (dropQueue.Count == 0)
                {
                    ISLogger.Write("Could not start drag drop operation: drop queue is empty");
                    return;
                }

                reportedSuccess = false;

                if (dropQueue.TryDequeue(out IDataObject dropObject))
                {
                    if (dropObject == null)
                    {
                        ISLogger.Write("attempted to drop a null dataobject");
                        this.Hide();
                        return;
                    }

                    int[] droppedValue = new int[1];
                    dropSourceAllowDrop = false;
                    Task.Run(() => { Thread.Sleep(300); dropSourceAllowDrop = true; });
                    registeredDrop = true;

                    Guid operationId = ((InputshareDataObject)dropObject).OperationGuid;

                    Dropping = true;
                    Ole32.DoDragDrop(dropObject, this, (int)DragDropEffects.All, droppedValue);
                    Dropping = false;
                    if (droppedValue[0] == 0)
                        DragDropCancelled?.Invoke(this, null);
                    else if (!reportedSuccess)
                    {
                        reportedSuccess = true;
                        DragDropSuccess?.Invoke(this, null);
                    }
                        
                }
                else
                {
                    ISLogger.Write("Failed to get data object");
                }
                return;
            }
            base.WndProc(ref m);
        }

        public void InvokeDoDragDrop(ClipboardDataBase data)
        {
            this.Invoke(new Action(() => {
                registeredDrop = false;
                cancelDrop = false;
                DoDragDrop(data);
            }));
        }

        public void InvokeExitWindow()
        {
            this.Invoke(new Action(() =>
            {
                Close();
            }));
        }

        public void CancelDrop()
        {
            dropSourceAllowDrop = false;
            cancelDrop = true;
            outMan.Send(new ISInputData(ISInputCode.IS_KEYDOWN, (short)WindowsVirtualKey.Escape, 0));
            Thread.Sleep(50);
            outMan.Send(new ISInputData(ISInputCode.IS_KEYUP, (short)WindowsVirtualKey.Escape, 0));

        }

        private void DoDragDrop(ClipboardDataBase data)
        {
            InputshareDataObject nativeObject = null;
            try
            {
                nativeObject = new InputshareDataObject(data, true);
                reportedSuccess = false;
                dropQueue.Enqueue(nativeObject);

                nativeObject.DropSuccess += NativeObject_DropSuccess;
            }
            catch(Exception ex)
            {
                ISLogger.Write("Could not start drag drop operation: " + ex.Message);
                return;
            }

            
            this.Show();
            this.BringToFront();
            this.TopMost = true;
            GetCurrentCursorMonitorSize(out Size mSize, out Point mPos);
            this.SetDesktopLocation((int)mPos.X, (int)mPos.Y);
            this.Size = mSize;
            outMan.Send(new InputshareLib.Input.ISInputData(InputshareLib.Input.ISInputCode.IS_MOUSELDOWN, 0, 0));

            Task.Run(() => {
                Thread.Sleep(300); this.Invoke(new Action(() => {
                    if (!registeredDrop)
                    {
                        DoDragDrop(data);
                    }
                }));
            });
        }

        private void NativeObject_DropSuccess(object sender, EventArgs e)
        {
            if (!reportedSuccess)
            {
                InputshareDataObject obj = (InputshareDataObject)sender;

                reportedSuccess = true;
                DragDropSuccess?.Invoke(this, null);
            }
        }

        private void WindowsDragSource_Load(object sender, EventArgs e)
        {
            this.SetDesktopLocation(0, 0);
            this.Height = 0;
            this.Width = 0;
            this.AllowDrop = true;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.WindowState = FormWindowState.Normal;
            this.Visible = false;
            this.AllowTransparency = true;
            this.TransparencyKey = System.Drawing.Color.Red;
            this.BackColor = System.Drawing.Color.Red;
            HandleCreatedEvent.Set();
        }

        private void GetCurrentCursorMonitorSize(out Size size, out Point pt)
        {
            IntPtr monPtr = MonitorFromPoint(new POINT(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y), 0x00000002);
            MONITORINFOEX monitor = new MONITORINFOEX();
            monitor.Size = Marshal.SizeOf(monitor);
            GetMonitorInfo(monPtr, ref monitor);
            size = new Size(Math.Abs(monitor.Monitor.right - monitor.Monitor.left),
                Math.Abs(monitor.Monitor.top - monitor.Monitor.bottom));

            pt = new Point(monitor.Monitor.left, monitor.Monitor.top);
        }

        int IDropSource.QueryContinueDrag(int fEscapePressed, uint grfKeyState)
        {
            this.Hide();
            this.Visible = false;
            registeredDrop = true;
            if (cancelDrop)
                return DRAGDROP_S_CANCEL;

            if (!dropSourceAllowDrop)
                return S_OK;
            var escapePressed = (0 != fEscapePressed);

            var keyStates = (DragDropKeyStates)grfKeyState;

            if (escapePressed && !cancelDrop)
            {
                return DRAGDROP_S_CANCEL;
            }
            else if ((DragDropKeyStates.None == (keyStates & DragDropKeyStates.LeftMouseButton)))
            {
                return DRAGDROP_S_DROP;
            }
            return S_OK;
        }

        int IDropSource.GiveFeedback(uint dwEffect)
        {
            return DRAGDROP_S_USEDEFAULTCURSORS;
        }
    }
}
