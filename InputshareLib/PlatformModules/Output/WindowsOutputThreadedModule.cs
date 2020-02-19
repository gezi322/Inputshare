using InputshareLib.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib.PlatformModules.Output
{
    internal class WindowsOutputThreadedModule : OutputModuleBase
    {
        private Thread _thread;
        private CancellationTokenSource _tokenSource;
        private BlockingCollection<InputData> _queue;
        private WindowsOutputModule _defaultOutputModule;

        public override void SimulateInput(ref InputData input)
        {
            _queue.Add(input);
        }

        private void ThreadLoop()
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                var input = _queue.Take(_tokenSource.Token);
                _defaultOutputModule.SimulateInput(ref input);
            }
        }

        protected override Task OnStart()
        {
            _defaultOutputModule = new WindowsOutputModule();
            _queue = new BlockingCollection<InputData>();
            _tokenSource = new CancellationTokenSource();
            _thread = new Thread(ThreadLoop);
            _thread.Priority = ThreadPriority.Highest;
            _thread.Start();

            return Task.CompletedTask;
        }

        protected override Task OnStop()
        {
            _tokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}
