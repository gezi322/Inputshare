using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Inputshare.Views
{
    public class ServerView : UserControl
    {
        public ServerView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
