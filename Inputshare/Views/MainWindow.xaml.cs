using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Inputshare.ViewModels;

namespace Inputshare.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = new MainWindowViewModel();
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            (DataContext as MainWindowViewModel).HandleExit();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
