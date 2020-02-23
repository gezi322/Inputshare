using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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

            _serverVM.Leave += OnViewModelLeave;
            _clientVM.Leave += OnViewModelLeave;
        }


        private void OnServerVMLeave(object sender, EventArgs e)
        {
            SetViewModel(_homeVM);
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
            CurrentView.OnShow();
            this.RaisePropertyChanged(nameof(CurrentView));
        }

        public override async Task HandleBottomButtonPressAsync()
        {
            await CurrentView.HandleBottomButtonPressAsync();
        }

        public override async Task HandleWindowClosingAsync()
        {
            await CurrentView.HandleWindowClosingAsync();
        }
    }
}
