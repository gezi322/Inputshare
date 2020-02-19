using InputshareLib.PlatformModules.Windows.Native;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputshareLib.PlatformModules.Windows
{
    //temp
    internal class Win32MessageForm : IDisposable
    {
        internal event EventHandler<Win32Message> MessageRecevied;
        internal IntPtr Handle => _form.Handle;
        private InnerForm _form;
        private Thread _wndThread;
        private SemaphoreSlim _creationWaitHandle;

        internal static async Task<Win32MessageForm> CreateAsync()
        {
            Win32MessageForm instance = new Win32MessageForm();
            instance._creationWaitHandle = new SemaphoreSlim(0, 1);

            instance._wndThread = new Thread(() => instance.InitForm());
            instance._wndThread.SetApartmentState(ApartmentState.STA);
            instance._wndThread.IsBackground = false;
            instance._wndThread.Start();

            if (!await instance._creationWaitHandle.WaitAsync(5000))
                throw new Exception("Timed out waiting for fomr creation event");

            return instance;
        }

        private void InitForm()
        {
            _form = new InnerForm(this);
            Application.Run(_form);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            _creationWaitHandle.Release();
        }

        internal void InvokeAction(Action invoke)
        {
            _form.Invoke(invoke);
        }

        private class InnerForm : Form
        {
            private Win32MessageForm _host;

            public InnerForm(Win32MessageForm host)
            {
                _host = host;
                this.Load += InnerForm_Load;
            }

            private void InnerForm_Load(object sender, EventArgs e)
            {
                _host.OnFormLoad(sender, e);
                this.Visible = false;
                this.ShowInTaskbar = false;
            }

            protected override void WndProc(ref Message m)
            {
                Win32Message msg = new Win32Message
                {
                    hwnd = m.HWnd,
                    lParam = m.LParam,
                    message = (uint)m.Msg,
                    wParam = m.WParam
                };

                _host.MessageRecevied?.Invoke(this, msg);
                base.WndProc(ref m);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _form?.Close();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
