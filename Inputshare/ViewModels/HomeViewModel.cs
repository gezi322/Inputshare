using Avalonia;
using Avalonia.Controls;
using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Inputshare.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public override event EventHandler Leave;

        public override string BottomButtonText { get; protected set; } = "Exit";
        public ReactiveCommand<Unit, Unit> CommandSelectClient { get; }
        public ReactiveCommand<Unit, Unit> CommandSelectServer { get; }


        public event EventHandler SelectClient;
        public event EventHandler SelectServer;

        public HomeViewModel()
        {
            CommandSelectServer = ReactiveCommand.Create(OnServerSelected);
            CommandSelectClient = ReactiveCommand.Create(OnClientSelected);
        }

        private void OnClientSelected()
        {
            SelectClient?.Invoke(this, null);
        }

        private void OnServerSelected()
        {
            SelectServer?.Invoke(this, null);
        }
        

        public override void OnBottomButtonPress()
        {
            //Exit program
            Leave?.Invoke(this, null);
        }

        public override void HandleWindowClosing()
        {
            
        }
    }
}
