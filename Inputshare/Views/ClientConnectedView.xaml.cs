using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using InputshareLib.Client;

namespace Inputshare.Views
{
    public class ClientConnectedView : UserControl
    {
        public ClientConnectedView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
