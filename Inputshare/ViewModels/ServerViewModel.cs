using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.ViewModels
{
    public class ServerViewModel : ViewModelBase
    {
        public override event EventHandler Leave;

        private string _t;
        public override string BottomButtonText { get => SelectedView.BottomButtonText; protected set => _t = value; }

        public ViewModelBase SelectedView { get; protected set; }

        private ServerRunningViewModel _runningView;
        private ServerStoppedViewModel _stoppedView;
        private ServerModel _model;

        public ServerViewModel()
        {
            _model = new ServerModel();
            _runningView = new ServerRunningViewModel(_model);
            _stoppedView = new ServerStoppedViewModel(_model);
            SelectedView = _stoppedView;

            _model.Started += OnServerStarted;
            _model.Stopped += OnServerStopped;
        }

        private void OnServerStopped(object sender, EventArgs e)
        {
            SetViewModel(_stoppedView);
        }

        private void OnServerStarted(object sender, EventArgs e)
        {
            SetViewModel(_runningView);
        }

        private void SetViewModel(ViewModelBase vm)
        {
            SelectedView = vm;
            this.RaisePropertyChanged(nameof(SelectedView));
            this.RaisePropertyChanged(nameof(BottomButtonText));
        }

        public override void HandleWindowClosing()
        {
            if (_model.Running)
                _model.StopAsync().Wait();
        }

        public override void OnBottomButtonPress()
        {
            if (SelectedView == _runningView)
                _runningView.OnBottomButtonPress();
            else
                Leave?.Invoke(this, null);
        }
    }
}
