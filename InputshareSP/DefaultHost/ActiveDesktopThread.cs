using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InputshareLib;
using InputshareLibWindows;
using InputshareLibWindows.Windows;

namespace InputshareSP.DefaultHost
{
    internal class ActiveDesktopThread : IDisposable
    {
       
        private BlockingCollection<Action> invokeQueue = new BlockingCollection<Action>();
        private Task deskThread;
        private CancellationTokenSource deskThreadCancelToken;
        private bool switchDesktop = false;

        internal ActiveDesktopThread()
        {
            deskThreadCancelToken = new CancellationTokenSource();
            deskThread = new Task(ThreadInit, deskThreadCancelToken.Token);
            deskThread.Start();
        }

        internal void Invoke(Action invoke)
        {
            invokeQueue.Add(invoke);
        }

        private void ThreadInit()
        {
            ISLogger.Write("Current desktop thread started");
            while (!deskThreadCancelToken.IsCancellationRequested)
            {
                try
                {
                    Action method = invokeQueue.Take();

                    if (switchDesktop)
                    {
                        switchDesktop = false;
                        SwitchToInputDesktop();
                    }

                    method();
                }
                catch(Exception ex)
                {
                    ISLogger.Write("ActiveDesktopThread: Invoked method threw exception:" + ex.Message);
                    ISLogger.Write(ex.StackTrace);
                }
            }
        }

        private void SwitchToInputDesktop()
        {
            try
            {
                Desktop.SwitchDesktop(Desktop.InputDesktop);
                ISLogger.Write("Switched to destkop {0}", Desktop.CurrentDesktop);
            }
            catch (Exception ex)
            {
                ISLogger.Write("Failed to switch thread desktop: " + ex.Message);
            }
        }

        public void InvokeSwitchDesktop()
        {
            switchDesktop = true;

            if (invokeQueue.Count == 0)
                invokeQueue.Add(() => { return; });
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    deskThreadCancelToken?.Cancel();
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
