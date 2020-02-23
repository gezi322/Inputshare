using Avalonia;
using Avalonia.Controls;
using Inputshare.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Inputshare.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public override event EventHandler Leave;

        public override string BottomButtonText { get; protected set; } = "Exit";

        public event EventHandler SelectClient;
        public event EventHandler SelectServer;

        public HomeViewModel()
        {
        }

        private void OnClientSelected()
        {
            SelectClient?.Invoke(this, null);
        }

        private void OnServerSelected()
        {
            SelectServer?.Invoke(this, null);
        }
        

        public override Task HandleBottomButtonPressAsync()
        {
            //Exit program
            Leave?.Invoke(this, null);

            return Task.CompletedTask;
        }

        public override Task HandleWindowClosingAsync()
        {
            return Task.CompletedTask;
        }
    }
}
