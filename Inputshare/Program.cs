using System;
using Avalonia;
using Avalonia.Logging.Serilog;
using Inputshare.ViewModels;
using Inputshare.Views;
using InputshareLib;

namespace Inputshare
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp().Start(AppMain, args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug()
                .UseReactiveUI();

        // Your application's entry point. Here you can initialize your MVVM framework, DI
        // container, etc.
        private static void AppMain(Application app, string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            StartOptions options = new StartOptions(new System.Collections.Generic.List<string>(args));

            if (options.HasArg(StartArguments.NoGui))
            {
                new Cli.CliMain(options);
                return;
            }

            var window = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            app.Run(window);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception; 

            ISLogger.Write("--------------------------------------");
            ISLogger.Write("UNHANDLED EXCEPTION");
            ISLogger.Write(ex.Message);
            ISLogger.Write(ex.StackTrace);
            ISLogger.Write(ex.Source);
            ISLogger.Write("--------------------------------------");
        }
    }
}
