using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Inputshare.Tray;
using Inputshare.ViewModels;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Inputshare.Views
{
    public class MainWindow : Window
    {
        private ITrayIcon _trayIcon;
        private bool _usingTrayIcon = true;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif


            try
            {
                CreateTrayIcon();
            }catch(Exception ex)
            {
                _usingTrayIcon = false;
                Console.WriteLine("Failed to create tray icon: " + ex.Message);
            }
            
        }

        private void CreateTrayIcon()
        {
            using (Bitmap icon = (Bitmap)Bitmap.FromFile(@"./assets/test.ico"))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    _trayIcon = WinTrayIcon.Create(icon);
                else
                    throw new PlatformNotSupportedException();
            }

            if (_usingTrayIcon)
            {
                _trayIcon.TrayIconClicked += Icon_TrayIconClicked;
            }
        }

        private void Icon_TrayIconClicked(object sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.ShowInTaskbar = true;
                this.WindowState = WindowState.Normal;
                this.BringIntoView();
                this.Focus();
            });
        }

        protected override void HandleWindowStateChanged(WindowState state)
        {
            if (_usingTrayIcon && state == WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
            }

            base.HandleWindowStateChanged(state);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override bool HandleClosing()
        {
            if(_usingTrayIcon)
                _trayIcon?.Dispose();

            (this.DataContext as MainWindowViewModel).HandleWindowClosingAsync();
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
