using Avalonia.Threading;
using InputshareLib;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.ViewModels
{
    internal class LogWindowViewModel : ViewModelBase
    {
        public string LogText { get; private set; } = "";

        public LogWindowViewModel()
        {
            ISLogger.LogMessageOut += ISLogger_LogMessageOut;
        }

        private async void ISLogger_LogMessageOut(object sender, string e)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                await Dispatcher.UIThread.InvokeAsync(() => { ISLogger_LogMessageOut(sender, e); });
                return;
            }

            LogText = LogText + e + Environment.NewLine;
            this.RaisePropertyChanged(nameof(LogText));
        }

        public override void HandleBottomButtonPressed()
        {

        }

        public override void HandleExit()
        {

        }
    }
}
