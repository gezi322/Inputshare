using Inputshare.Common.Server;
using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Inputshare.ViewModels
{
    public class ServerRunningViewModel : ViewModelBase
    {
        public override string BottomButtonText { get; protected set; } = "Stop server";
        public ObservableCollection<ServerDisplayModel> ServerDisplaysList { get; }
        public ObservableCollection<ServerDisplayModel> ServerSelectableDisplayList { get; } = new ObservableCollection<ServerDisplayModel>();

        private ServerDisplayModel _selectedDisplay = ServerDisplayModel.None;
        public ServerDisplayModel SelectedDisplay { get => _selectedDisplay; set => OnSelectedDisplayChange(value); }

        private ServerModel _model;

        public ServerRunningViewModel(ServerModel model)
        {
            _model = model;
            _model.Stopped += OnServerStopped;
            ServerDisplaysList = _model.Displays;
            ServerDisplaysList.CollectionChanged += OnServerDisplayListChanged;
            BuildSelectableDisplayList();
        }

        private void OnServerStopped(object sender, EventArgs e)
        {
            
        }

        private void OnServerDisplayListChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            BuildSelectableDisplayList();
        }

        private void OnSelectedDisplayChange(ServerDisplayModel display)
        {
            _selectedDisplay = display;
            BuildSelectableDisplayList();
            this.RaisePropertyChanged(nameof(SelectedDisplay));
        }

        private void BuildSelectableDisplayList()
        {
            ServerSelectableDisplayList.Clear();
            ServerSelectableDisplayList.Add(ServerDisplayModel.None);

            foreach (var display in ServerDisplaysList)
                ServerSelectableDisplayList.Add(display);

            if (SelectedDisplay != null && SelectedDisplay != ServerDisplayModel.None)
                ServerSelectableDisplayList.Remove(SelectedDisplay);
        }

        public override void HandleWindowClosing()
        {

        }

        public override async void OnBottomButtonPress()
        {
            if (_model.Running)
                await _model.StopAsync();
        }
    }
}
