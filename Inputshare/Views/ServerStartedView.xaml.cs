using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Inputshare.ViewModels;

namespace Inputshare.Views
{
    public class ServerStartedView : UserControl
    {
        public ServerStartedView()
        {
            this.InitializeComponent();
            this.KeyDown += ServerStartedView_KeyDown;
        }

        private void ServerStartedView_KeyDown(object sender, Avalonia.Input.KeyEventArgs e)
        {
            (this.DataContext as ServerStartedViewModel).HandleKeyDown(e);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
