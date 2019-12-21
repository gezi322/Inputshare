using InputshareLib;
using InputshareLibWindows.Clipboard;
using System;
using System.Threading;
using System.Windows.Forms;

namespace InputshareLibWindows
{

    /// <summary>
    /// Represents a message only window. Used for keyboard, mouse and clipboard event hooks.
    /// Hooks should only be installed after the HandleCreated event has been fired
    /// 
    /// InitWindow must be called before calling any public methods.
    /// </summary>
    public class MessageWindow
    {
        public event EventHandler HandleCreated;
        public event EventHandler WindowDestroyed;

        public bool Closed { get; protected set; } = false;

        private Thread wndThread;
        private CancellationTokenSource cancelToken;

        protected IntPtr Handle { get; private set; }
        protected string WindowName { get; }

        protected InnerForm innerForm;
        protected AutoResetEvent windowHandleCreateEvent = new AutoResetEvent(false);
        private AutoResetEvent windowDestroyEvent = new AutoResetEvent(false);

        public MessageWindow(string wndName)
        {
            WindowName = wndName;
        }

        private bool initCalled = false;
        public void InitWindow()
        {
            if (initCalled)
                return;
            windowHandleCreateEvent = new AutoResetEvent(false);

            initCalled = true;
            cancelToken = new CancellationTokenSource();
            wndThread = new Thread(() =>
            {
                CreateWindow();
            });
            wndThread.SetApartmentState(ApartmentState.STA);
            wndThread.Start();

            if (!windowHandleCreateEvent.WaitOne(5000))
                throw new Exception("Timed out waiting for window handle creation");
        }

        public virtual void CloseWindow()
        {
            InvokeAction(() => { innerForm.Close(); });
            windowDestroyEvent.WaitOne(1000);
        }
       
        public virtual void SetClipboardData(InputshareDataObject data)
        {
            if (Closed)
                throw new InvalidOperationException("Window has been closed");

            InvokeAction(new Action(() => {
                for(int i = 0; i < 10; i++)
                {
                    try
                    {
                        System.Windows.Clipboard.SetDataObject(data);
                        return;
                    }catch
                    {
                        Thread.Sleep(25);
                    }
                }

                ISLogger.Write("Failed to set clipboard data!");
            }));
        }

        private void CreateWindow()
        {
            innerForm = new InnerForm(WndProc);
            innerForm.Load += InnerForm_Load;
            innerForm.FormClosed += InnerForm_FormClosed;
            System.Windows.Forms.Application.Run(innerForm);
        }

        private void InnerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            windowDestroyEvent.Set();
            WindowDestroyed?.Invoke(this, new EventArgs());
        }

        private void InnerForm_Load(object sender, EventArgs e)
        {
            Handle = innerForm.Handle;
            windowHandleCreateEvent.Set();
            HandleCreated?.Invoke(this, e);
        }

        protected virtual bool WndProc(ref Message msg)
        {

            return false;
        }


        protected void InvokeAction(Action invoke)
        {
            if (Closed)
                throw new InvalidOperationException("Window has been closed");

            if (Handle == IntPtr.Zero)
                throw new InvalidOperationException("Window handle does not exist");

            innerForm.Invoke(invoke);
        }

        //The old nativewindow implementation was causing issues with OLE dataobjects and crashing some applications
        //TODO - reimplement native window and remove the need for Sdk="Microsoft.NET.Sdk.WindowsDesktop"
        protected class InnerForm : Form
        {
            public delegate bool FormMessageHandler(ref Message m);
            private FormMessageHandler messageHandler;

            public InnerForm(FormMessageHandler handler)
            {
                messageHandler = handler;
                Load += InnerForm_Load;
            }

            private void InnerForm_Load(object sender, EventArgs e)
            {
                this.Hide();
                this.Visible = false;
                this.ShowInTaskbar = false;
            }

            protected override void WndProc(ref Message m)
            {
                if(!messageHandler(ref m))
                    base.WndProc(ref m);
            }
        }
    }
}
