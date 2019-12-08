using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using InputshareB.ViewModels;

namespace InputshareB.Views
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
