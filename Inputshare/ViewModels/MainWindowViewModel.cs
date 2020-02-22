using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public override event EventHandler Leave;

        public ViewModelBase CurrentView { get; private set; }
        public override string BottomButtonText { get; protected set; } = "";

        private HomeViewModel _homeVM = new HomeViewModel();
        private ClientViewModel _clientVM = new ClientViewModel();
        private ServerViewModel _serverVM = new ServerViewModel();

        public MainWindowViewModel() {
            _homeVM = new HomeViewModel();
            _homeVM.Leave += OnHomeVMLeave;
            CurrentView = _homeVM;

            _homeVM.SelectClient += OnHomeClientSelected;
            _homeVM.SelectServer += OnHomeServerSelected;
            _clientVM.Leave += OnViewModelLeave;
        }

       

        private void OnHomeVMLeave(object sender, EventArgs e)
        {
            Leave?.Invoke(this, null);
        }

        private void OnViewModelLeave(object sender, EventArgs e)
        {
            SetViewModel(_homeVM);
        }

        private void OnHomeClientSelected(object sender, EventArgs e)
        {
            SetViewModel(_clientVM);
        }

        private void OnHomeServerSelected(object sender, EventArgs e)
        {
            SetViewModel(_serverVM);
        }

        private void SetViewModel(ViewModelBase vm)
        {
            CurrentView = vm;
            this.RaisePropertyChanged(nameof(CurrentView));
        }

        public override void OnBottomButtonPress()
        {
            CurrentView.OnBottomButtonPress();
        }

        public override void HandleWindowClosing()
        {
            CurrentView.HandleWindowClosing();
        }
    }
}
