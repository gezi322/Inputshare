using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Inputshare.Views
{
    public class WinServiceBaseView : UserControl
    {
        public WinServiceBaseView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
