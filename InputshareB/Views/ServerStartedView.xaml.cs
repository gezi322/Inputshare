using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace InputshareB.Views
{
    public class ServerStartedView : UserControl
    {
        public ServerStartedView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
