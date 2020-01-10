using Inputshare.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.ViewModels
{
    internal class ClientConnectedViewModel : ViewModelBase
    {
        private ISClientModel model;

        public string InfoText { get; protected set; }

        public ClientConnectedViewModel(ISClientModel model)
        {
            this.model = model;
            BottomButtonText = "Disconnect";
            model.Connected += Model_Connected;
        }

        private void Model_Connected(object sender, EventArgs e)
        {
            InfoText = string.Format("Connected to {0} as {1}", model.ConnectedAddress, model.ClientName);
        }

        public override void HandleBottomButtonPressed()
        {
            model.Disconnect();
        }

        public override void HandleExit()
        {
            model.StopIfRunning();
        }
    }
}
