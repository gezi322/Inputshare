using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Inputshare.Views
{
    public class WinServiceDisconnectedView : UserControl
    {
        public WinServiceDisconnectedView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
