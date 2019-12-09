using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Inputshare.ViewModels;

namespace Inputshare.Views
{
    public class ModeSelectView : UserControl
    {
        public ModeSelectView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
