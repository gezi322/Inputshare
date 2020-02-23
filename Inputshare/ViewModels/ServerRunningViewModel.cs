using Inputshare.Common.Input.Keys;
using Inputshare.Common.Server;
using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
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

        public ReactiveCommand<Unit, Unit> CommandSetHotkey { get; private set; }

        public List<WindowsVirtualKey> PossibleKeys { get; }  = new List<WindowsVirtualKey>();
        
        private ServerModel _model;

        public ServerRunningViewModel(ServerModel model)
        {
            _model = model;
            _model.Stopped += OnServerStopped;
            ServerDisplaysList = _model.Displays;
            ServerDisplaysList.CollectionChanged += OnServerDisplayListChanged;
            BuildSelectableDisplayList();
            BuildPossibleKeyList();

            CommandSetHotkey = ReactiveCommand.Create(ExecSetHotkey);
        }

        private void ExecSetHotkey()
        {
            SelectedDisplay.PushHotkey();
        }

        private void BuildPossibleKeyList()
        {
            foreach (var key in (WindowsVirtualKey[])Enum.GetValues(typeof(WindowsVirtualKey)))
            {
                PossibleKeys.Add(key);
                Console.WriteLine("Added key " + key);
            }
        }

        private void OnServerStopped(object sender, EventArgs e)
        {
            SelectedDisplay = ServerDisplayModel.None;
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
