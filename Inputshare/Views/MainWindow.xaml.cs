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
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.KeyDown += MainWindow_KeyDown;
            this.Closing += MainWindow_Closing;
            this.DataContextChanged += MainWindow_DataContextChanged;
            
        }

        private void MainWindow_DataContextChanged(object sender, System.EventArgs e)
        {
            (DataContext as MainWindowViewModel).CloseWindow += MainWindow_CloseWindow;
        }

        private void MainWindow_CloseWindow(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            (DataContext as MainWindowViewModel).HandleExit();
        }

        private void MainWindow_KeyDown(object sender, Avalonia.Input.KeyEventArgs e)
        {
            (DataContext as MainWindowViewModel).CurrentView.HandleKeyPress(e);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
