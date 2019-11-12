using InputshareLibWindows.Output;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static InputshareLibWindows.Native.User32;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace InputshareLibWindows.PlatformModules.DragDrop
{
    class WindowsDropTarget : Form
    {
        /// <summary>
        /// This event gets set when the window is loaded.
        /// </summary>
        public AutoResetEvent HandleCreatedEvent = new AutoResetEvent(false);

        /// <summary>
        /// Occurs when a user drops a file into the window.
        /// To check if the user has dragged a file off screen, use CheckForFileDrop()
        /// and subscribe to this event
        /// </summary>
        public event EventHandler<System.Windows.Forms.IDataObject> DataDropped;

        /// <summary>
        /// Creates a new hidden instance of a dragdrop window
        /// </summary>
        /// <param name="handleCreatedEvent">An event that will be set when the window is fully loaded</param>

        public readonly AutoResetEvent WindowClosedEvent = new AutoResetEvent(false);

        public bool InputshareDataDropped { get; set; } = false;

        public bool CurrentlyDragging { get; set; } = false;

        public WindowsDropTarget(AutoResetEvent handleCreatedEvent)
        {
            HandleCreatedEvent = handleCreatedEvent;
            this.FormClosed += WindowsDropTarget_FormClosed;
            this.Width = 0;
            this.Height = 0;
            this.SetDesktopLocation(0, 0);
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.AllowDrop = true;
            this.ShowIcon = false;
            this.DragOver += WindowsDropForm_DragOver;
            this.DragDrop += WindowsDropForm_DragDrop;
            this.Shown += WindowsDropForm_Shown;
            this.Load += WindowsDropForm_Load;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.WindowState = FormWindowState.Normal;
            this.AllowTransparency = true;
            this.TransparencyKey = System.Drawing.Color.Red;
            this.BackColor = System.Drawing.Color.Red;
        }

        private void WindowsDropTarget_FormClosed(object sender, FormClosedEventArgs e)
        {
            WindowClosedEvent.Set();
        }

        readonly WindowsOutputManager outM = new WindowsOutputManager();

        /// <summary>
        /// Occurs when the window has loaded, we set the HandleCreatedEvent to let
        /// the class owner know that it is ready to be used
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowsDropForm_Load(object sender, EventArgs e)
        {
            HandleCreatedEvent.Set();
        }

        private void WindowsDropForm_Shown(object sender, EventArgs e)
        {
            this.Hide();
        }

        /// <summary>
        /// Checks if the user has dragged a file off screen.
        /// The DataDropped event will be fired if a file drop is detected
        /// </summary>
        public void CheckForDrop()
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() => { CheckForDrop(); }));
                return;
            }
            AllowDrop = true;
            this.BringToFront();
            this.TopMost = true;
            
            this.SetDesktopLocation(System.Windows.Forms.Cursor.Position.X-40, System.Windows.Forms.Cursor.Position.Y-40);

            GetCurrentCursorMonitorSize(out Size mSize, out Point mPos);
            this.SetDesktopLocation((int)mPos.X, (int)mPos.Y);
            this.Size = mSize;
            this.Show();
            Task.Run(() => { Thread.Sleep(200); this.Invoke(new Action(() => { this.Hide(); this.Visible = false; })); });
        }

        public void InvokeExitWindow()
        {
            this.Invoke(new Action(() =>
            {
                Close();
            }));
        }

        /// <summary>
        /// Gets the position and size of the monitor that the cursor is current on.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pt"></param>
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

        /// <summary>
        /// Occurs when a user drags a file onto the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void WindowsDropForm_DragDrop(object sender, System.Windows.Forms.DragEventArgs args)
        {
            AllowDrop = false;
            this.Hide();
            this.Visible = false;
            this.Width = 0;
            this.Height = 0;
            this.SetDesktopLocation(0, 0);

            //We need to make sure that we don't send the dragdrop operation directly back to the server
            bool isInputshareDrop = false;
            foreach (var format in args.Data.GetFormats())
            {
                if (format.Contains("Inputshare"))
                    isInputshareDrop = true;
            }
           
            if (isInputshareDrop)
            {
                InputshareDataDropped = true;
                return;
            }

            args.Effect = System.Windows.Forms.DragDropEffects.Copy;
            //TODO - Decide whether to copy or move files/image/text
            /*
            if (Convert.ToBoolean(GetKeyState(Keys.Control) & 0x8000))
            {
                ISLogger.Write("dragdrop mode: Copy");
                args.Effect = System.Windows.Forms.DragDropEffects.Copy;
            }
            else
            {
                ISLogger.Write("dragdrop mode: Move");
                args.Effect = System.Windows.Forms.DragDropEffects.Move;
            }*/


            DataDropped?.Invoke(this, args.Data);
        }

        /// <summary>
        /// Occurs when the user drags an item over the the window.
        /// Here we use WindowsOutputManager to release the left mouse button
        /// so that the data is dropped, even if the input is currently being redirectd
        /// to another client.
        /// </summary>
        /// <param name="sender">This form</param>
        /// <param name="args"></param>
        private void WindowsDropForm_DragOver(object sender, System.Windows.Forms.DragEventArgs args)
        {
            if (!AllowDrop)
            {
                args.Effect = 0;
                return;
            }

            args.Effect = System.Windows.Forms.DragDropEffects.Copy;
            outM.Send(new InputshareLib.Input.ISInputData(InputshareLib.Input.ISInputCode.IS_MOUSELUP, 0, 0));
        }
    }
}
