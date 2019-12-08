using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib;
using InputshareLib.Client;
using InputshareLib.Server;
using ReactiveUI;

namespace Inputshare.ViewModels
{
    public class ServerStoppedViewModel : ViewModelBase
    {
        private ISServer serverInstance;

        //Commands
        public ReactiveCommand StartServer { get; private set; }

        private string portEntryText = "";

        private bool canExecute;
        public bool CanExecute { get { return canExecute; } set { this.RaiseAndSetIfChanged(ref canExecute, value); } }
        public string PortEntryText { get => portEntryText; set => SetText(value); }


        public bool EnableUdpChecked { get; set; } = true;
        public bool EnableClipboardChecked { get; set; } = true;
        public bool EnableDragDropChecked { get; set; } = true;

        public ServerStoppedViewModel(ISServer server)
        {
            serverInstance = server;
            SetText("4441");
            InitCommands();
        }

        private void SetText(string text)
        {
            CanExecute = int.TryParse(text, out _);
            portEntryText = text;
        }

        private void InitCommands()
        {
            StartServer = ReactiveCommand.Create(ExecStartServer);
        }

        private void ExecStartServer()
        {
            if (!CanExecute)
                return;

            List<string> options = new List<string>();
            if(!EnableDragDropChecked)
                options.Add("NoDragDrop");
            if(!EnableClipboardChecked)
                options.Add("NoClipboard");
            if (!EnableUdpChecked)
                options.Add("NoUdp");

            serverInstance.SetStartArgs(new StartOptions(options));
            serverInstance.Start(int.Parse(PortEntryText));
        }

    }
}
