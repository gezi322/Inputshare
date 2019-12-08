using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using InputshareLib.Client;

namespace InputshareB.Views
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
