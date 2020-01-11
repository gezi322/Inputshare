using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using InputshareLib;
using InputshareLib.Server;

namespace Inputshare
{
    class Program
    {
        public static void Main(string[] args)
        {
            ISLogger.PrefixCaller = false;
            ISLogger.PrefixTime = true;
            ISLogger.EnableConsole = true;
            ISLogger.EnableLogFile = true;
            ISLogger.SetLogFileName("Inputshare.log");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ISLogger.Write("UNHANDLED EXCEPTION!");
            Exception ex = e.ExceptionObject as Exception;
            while (ex.InnerException != null)
                ex = ex.InnerException;

            ISLogger.Write(ex.Message);
            ISLogger.Write(ex.Source);
            ISLogger.Write(ex.StackTrace);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug()
                .UseReactiveUI();
    }
}
