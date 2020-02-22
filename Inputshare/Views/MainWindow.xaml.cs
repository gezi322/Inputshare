using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Inputshare.ViewModels;
using System;

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

        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override bool HandleClosing()
        {
            (this.DataContext as MainWindowViewModel).HandleWindowClosing();
            return base.HandleClosing();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            (this.DataContext as MainWindowViewModel).Leave += MainWindow_Leave;
        }


        private void MainWindow_Leave(object sender, System.EventArgs e)
        {
            this.Close();
        }
    }
}
