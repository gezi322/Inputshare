using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace InputshareB.Views
{
    public class WindowsServiceView : UserControl
    {
        public WindowsServiceView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
