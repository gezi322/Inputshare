using InputshareLib;
using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.Windows;
using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;

namespace InputshareSP
{
    public sealed class InputshareSP
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            ISLogger.SetLogFileName("InputshareSPmain.log");
            ISLogger.EnableConsole = true;
            ISLogger.EnableLogFile = true;
            ISLogger.PrefixTime = true;

            PrintInfo();

            if (args.Length != 3)
            {
                OnInvalidArgs();
                return;
            }

            string mode = args[0];
            string readPipe = args[1];
            string writePipe = args[2];

            AnonIpcClient iClient = new AnonIpcClient(writePipe, readPipe, "ServiceIpc");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (!iClient.Connected)
            {
                if(sw.ElapsedMilliseconds > 2000)
                {
                    ISLogger.Write("Failed to connect to service IPC... exiting");
                    Thread.Sleep(2000);
                    return;
                }

                Thread.Sleep(100);
            }

            if (mode == "Default")
                new SPDefaultHost(iClient);
            else if (mode == "DragDrop")
                new SPDragDropHost(iClient);
            else
                OnInvalidArgs();

            return;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            ISLogger.Write("---------------------------------");
            ISLogger.Write("Unhandled exception!");
            ISLogger.Write(ex.Message);
            ISLogger.Write(ex.StackTrace);
            ISLogger.Write("---------------------------------");
        }

        private static void PrintInfo()
        {
            IntPtr token = IntPtr.Zero;
            try
            {
                ISLogger.Write("InputshareSP running as user " + Environment.UserName);
                token = Token.GetCurrentProcessToken();
                ISLogger.Write("Elevation type: " + Token.QueryElevation(token));
                ISLogger.Write("Session ID: " + Session.ConsoleSessionId);
                ISLogger.Write("Session state: " + Session.ConsoleSessionState);
                ISLogger.Write(@"Desktop: Winsta0\\" + Desktop.CurrentDesktop);
            }
            finally
            {
                if(token != IntPtr.Zero)
                    Token.CloseToken(token);
            }
        }

        static void OnInvalidArgs()
        {
            ISLogger.Write("InputshareSP started with invalid args...");
            Thread.Sleep(2000);
        }
    }
}
