using System;
using System.Diagnostics;
using System.Threading;
using InputshareLib;
using InputshareLibWindows.IPC.AnonIpc;

namespace InputshareSP
{
    class InputshareSP
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            ISLogger.SetLogFileName("InputshareSP.log");
            ISLogger.EnableConsole = true;
            ISLogger.EnableLogFile = true;

            if(args.Length != 3)
            {
                OnInvalidArgs();
                return;
            }

            string mode = args[0];
            string readPipe = args[1];
            string writePipe = args[2];

            AnonIpcClient client = new AnonIpcClient(readPipe, writePipe, "ServiceIPC");

            if (!client.ConnectedEvent.WaitOne(2000))
            {
                ISLogger.Write("Failed to connect to service... exiting");
                return;
            }

            ISLogger.Write("Connected to service IPC!");

            if (mode == "Default")
                DefaultHost.SPDefaultHost.Init(client);
            else if (mode == "Clipboard")
                ClipboardHost.SPClipboardHost.Init(client);
            else
                OnInvalidArgs();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            ISLogger.Write("---------------------------------");
            ISLogger.Write("Unhandled exception!");
            ISLogger.Write(ex.Message);
            ISLogger.Write(ex.StackTrace);
            ISLogger.Write("---------------------------------");
            Thread.Sleep(5000);
            Process.GetCurrentProcess().Kill();
        }

        static void OnInvalidArgs()
        {
            ISLogger.Write("SP started with invalid args");
            Thread.Sleep(20000);
            Console.ReadLine();
        }
    }
}
