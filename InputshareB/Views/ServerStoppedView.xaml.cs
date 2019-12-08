using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using InputshareB.ViewModels;

namespace InputshareB.Views
{
    public class ServerStoppedView : UserControl
    {
        public ServerStoppedView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
