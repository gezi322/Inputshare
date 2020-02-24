using System;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using Inputshare.Common.Client;
using Inputshare.Common.Input.Hotkeys;
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
                X11InputModule mod = new X11InputModule(con);
                await mod.StartAsync();
                mod.RegisterHotkey(new Hotkey(Common.Input.Keys.WindowsVirtualKey.B, KeyModifiers.Alt), () => {
                    Console.WriteLine("Hotkey pressed");
                });


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
