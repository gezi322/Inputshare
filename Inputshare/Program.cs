using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using InputshareLib;

namespace Inputshare
{
    class Program
    {
        public static void Main(string[] args)
        {
            ISLogger.PrefixCaller = false;
            ISLogger.PrefixTime = true;
            ISLogger.EnableConsole = true;
            ISLogger.EnableLogFile = false;
            ISLogger.Write("Inputshare started");

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug()
                .UseReactiveUI();
    }
}
