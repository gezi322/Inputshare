using System;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using Inputshare.Common.Client;
using Inputshare.Common.Clipboard;
using Inputshare.Common.Input.Hotkeys;
using Inputshare.Common.PlatformModules.Clipboard;
using Inputshare.Common.PlatformModules.Input;
using Inputshare.Common.PlatformModules.Linux;
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
                //ISServer s = new ISServer();
                //await s.StartAsync(new System.Net.IPEndPoint(IPAddress.Any, 4441));
                
                XConnection con = new XConnection();
                X11ClipboardModule mod = new X11ClipboardModule(con);
                await mod.StartAsync();
  
                mod.ClipboardChanged += (object o, ClipboardData cbData) =>
                {
                    Console.WriteLine("Copied");
                };

                ClipboardData cb = new ClipboardData();
                cb.SetText("TEST LOL");
                await mod.SetClipboardAsync(cb);


                Console.ReadLine();
                return;
            }else if (args.Length > 0 && args[0] == "ok")
            {

                ISClient c = new ISClient();
                await c.StartAsync();
                c.SetClientName(Environment.MachineName);
                await c.ConnectAsync(IPEndPoint.Parse("192.168.0.10:4441"));
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
