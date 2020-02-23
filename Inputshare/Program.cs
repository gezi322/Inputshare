using System;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using Inputshare.Common.Client;
using Inputshare.Common.Server;
using Inputshare.Tray;
using Serilog;

namespace Inputshare
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static async Task Main(string[] args)
        {
           

            if(args.Length > 0 && args[0] == "test")
            {
                ISServer s = new ISServer();
                await s.StartAsync(new System.Net.IPEndPoint(IPAddress.Any, 5555));

                Console.ReadLine();
                return;
            }else if (args.Length > 0 && args[0] == "ok")
            {
                ISClient c = new ISClient();
                await c.StartAsync();
                await c.ConnectAsync(IPEndPoint.Parse("192.168.0.26:5555"));
                Console.ReadLine();
                return;
            }



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
