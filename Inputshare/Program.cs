using System;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using Inputshare.Common.Client;
using Inputshare.Common.Clipboard;
using Inputshare.Common.PlatformModules.Linux;
using Inputshare.Common.PlatformModules.Linux.Modules;
using Inputshare.Common.Server;

namespace Inputshare
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
                BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug()
                .UseReactiveUI();
    }
}
