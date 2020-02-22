using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Inputshare.Views
{
    public class ClientView : UserControl
    {
        public ClientView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
